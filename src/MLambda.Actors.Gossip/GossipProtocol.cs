// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GossipProtocol.cs" company="MLambda">
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace MLambda.Actors.Gossip
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using MLambda.Actors.Gossip.Abstraction;
    using MLambda.Actors.Gossip.Messages;
    using MLambda.Actors.Network;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Core gossip protocol engine with SYN-ACK-ACK2 exchange.
    /// </summary>
    public class GossipProtocol : IDisposable
    {
        private readonly ClusterConfig config;
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;
        private readonly object stateLock = new object();

        private GossipState state;
        private IDisposable gossipTimer;
        private IDisposable messageSubscription;
        private Random random = new Random();
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="GossipProtocol"/> class.
        /// </summary>
        /// <param name="config">The cluster configuration.</param>
        /// <param name="transport">The transport layer.</param>
        /// <param name="serializer">The message serializer.</param>
        public GossipProtocol(ClusterConfig config, ITransport transport, IMessageSerializer serializer)
        {
            this.config = config;
            this.transport = transport;
            this.serializer = serializer;
            this.state = new GossipState();
        }

        /// <summary>
        /// Occurs when state changes are detected during gossip merge.
        /// </summary>
        public event Action<List<(Member Member, MemberStatus OldStatus)>> StateChanged;

        /// <summary>
        /// Gets the current gossip state.
        /// </summary>
        public GossipState CurrentState
        {
            get
            {
                lock (this.stateLock)
                {
                    return this.state;
                }
            }
        }

        /// <summary>
        /// Starts the gossip protocol.
        /// </summary>
        public void Start()
        {
            this.messageSubscription = this.transport.IncomingMessages
                .Where(e => e.Type == EnvelopeType.SystemMessage)
                .Subscribe(this.HandleGossipMessage);

            this.gossipTimer = Observable.Interval(this.config.GossipInterval)
                .Subscribe(_ => this.GossipRound());
        }

        /// <summary>
        /// Stops the gossip protocol.
        /// </summary>
        public void Stop()
        {
            this.gossipTimer?.Dispose();
            this.messageSubscription?.Dispose();
        }

        /// <summary>
        /// Updates the local state with a member.
        /// </summary>
        /// <param name="member">The member to update.</param>
        public void UpdateMember(Member member)
        {
            lock (this.stateLock)
            {
                this.state = this.state.SetMember(member);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes managed resources.
        /// </summary>
        /// <param name="disposing">True if disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed && disposing)
            {
                this.disposed = true;
                this.Stop();
            }
        }

        private void GossipRound()
        {
            List<Member> peers;
            lock (this.stateLock)
            {
                peers = this.state.Members.Values
                    .Where(m => m.Endpoint != this.config.LocalEndpoint
                        && m.Status != MemberStatus.Down
                        && m.Status != MemberStatus.Removed)
                    .ToList();
            }

            if (peers.Count == 0)
            {
                return;
            }

            var targets = peers.OrderBy(_ => this.random.Next())
                .Take(Math.Min(this.config.GossipFanout, peers.Count))
                .ToList();

            foreach (var target in targets)
            {
                this.SendGossipSyn(target.Endpoint);
            }
        }

        private void SendGossipSyn(NodeEndpoint target)
        {
            List<GossipDigest> digests;
            lock (this.stateLock)
            {
                digests = this.state.Members.Values.Select(m => new GossipDigest
                {
                    NodeId = m.Endpoint.NodeId,
                    Port = m.Endpoint.Port,
                    HeartbeatSequence = m.HeartbeatSequence,
                }).ToList();
            }

            var syn = new GossipSyn
            {
                SenderId = this.config.LocalEndpoint.NodeId,
                Digests = digests,
            };

            this.SendSystemMessage(target, syn);
        }

        private void HandleGossipMessage(Envelope envelope)
        {
            try
            {
                var payload = this.serializer.Deserialize(envelope.PayloadBytes, envelope.PayloadTypeName);

                switch (payload)
                {
                    case GossipSyn syn:
                        this.HandleSyn(syn, envelope.SourceNode);
                        break;
                    case GossipAck ack:
                        this.HandleAck(ack, envelope.SourceNode);
                        break;
                    case GossipAck2 ack2:
                        this.HandleAck2(ack2);
                        break;
                    case JoinRequest join:
                        this.HandleJoinRequest(join);
                        break;
                    case LeaveRequest leave:
                        this.HandleLeaveRequest(leave);
                        break;
                }
            }
            catch (Exception)
            {
            }
        }

        private void HandleSyn(GossipSyn syn, NodeEndpoint source)
        {
            var updatedMembers = new List<GossipMemberState>();
            var requestedDigests = new List<GossipDigest>();

            lock (this.stateLock)
            {
                foreach (var digest in syn.Digests)
                {
                    if (this.state.Members.TryGetValue(digest.EndpointKey, out var local))
                    {
                        if (local.HeartbeatSequence > digest.HeartbeatSequence)
                        {
                            updatedMembers.Add(ToMemberState(local));
                        }
                        else if (local.HeartbeatSequence < digest.HeartbeatSequence)
                        {
                            requestedDigests.Add(digest);
                        }
                    }
                    else
                    {
                        requestedDigests.Add(digest);
                    }
                }

                foreach (var kvp in this.state.Members)
                {
                    if (!syn.Digests.Any(d => d.EndpointKey == kvp.Key))
                    {
                        updatedMembers.Add(ToMemberState(kvp.Value));
                    }
                }
            }

            var ack = new GossipAck
            {
                SenderId = this.config.LocalEndpoint.NodeId,
                UpdatedMembers = updatedMembers,
                RequestedDigests = requestedDigests,
            };

            this.SendSystemMessage(source, ack);
        }

        private void HandleAck(GossipAck ack, NodeEndpoint source)
        {
            this.ApplyMemberUpdates(ack.UpdatedMembers);

            if (ack.RequestedDigests.Count > 0)
            {
                var members = new List<GossipMemberState>();
                lock (this.stateLock)
                {
                    foreach (var digest in ack.RequestedDigests)
                    {
                        if (this.state.Members.TryGetValue(digest.EndpointKey, out var member))
                        {
                            members.Add(ToMemberState(member));
                        }
                    }
                }

                var ack2 = new GossipAck2
                {
                    SenderId = this.config.LocalEndpoint.NodeId,
                    Members = members,
                };

                this.SendSystemMessage(source, ack2);
            }
        }

        private void HandleAck2(GossipAck2 ack2)
        {
            this.ApplyMemberUpdates(ack2.Members);
        }

        private void HandleJoinRequest(JoinRequest join)
        {
            var endpoint = new NodeEndpoint(join.NodeId, join.Port);
            var member = new Member(endpoint, MemberStatus.Joining);

            lock (this.stateLock)
            {
                this.state = this.state.SetMember(member);
            }

            this.StateChanged?.Invoke(new List<(Member, MemberStatus)> { (member, MemberStatus.Removed) });
        }

        private void HandleLeaveRequest(LeaveRequest leave)
        {
            lock (this.stateLock)
            {
                var key = $"{leave.NodeId}:{leave.Port}";
                if (this.state.Members.TryGetValue(key, out var member))
                {
                    var oldStatus = member.Status;
                    member.Status = MemberStatus.Leaving;
                    member.HeartbeatSequence++;
                    this.state = this.state.SetMember(member);
                    this.StateChanged?.Invoke(new List<(Member, MemberStatus)> { (member, oldStatus) });
                }
            }
        }

        private void ApplyMemberUpdates(List<GossipMemberState> updates)
        {
            if (updates.Count == 0)
            {
                return;
            }

            var remoteState = new GossipState();
            foreach (var ms in updates)
            {
                var endpoint = new NodeEndpoint(ms.NodeId, ms.Port);
                var member = new Member(endpoint, (MemberStatus)ms.Status)
                {
                    HeartbeatSequence = ms.HeartbeatSequence,
                    LastSeen = new DateTimeOffset(ms.LastSeenTicks, TimeSpan.Zero),
                };
                remoteState.Members[endpoint.ToString()] = member;
            }

            List<(Member Member, MemberStatus OldStatus)> changes;
            lock (this.stateLock)
            {
                (this.state, changes) = this.state.Merge(remoteState);
            }

            if (changes.Count > 0)
            {
                this.StateChanged?.Invoke(changes);
            }
        }

        private void SendSystemMessage(NodeEndpoint target, object message)
        {
            var envelope = new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetActorId = Guid.Empty,
                SourceActorId = Guid.Empty,
                SourceNode = this.config.LocalEndpoint,
                Type = EnvelopeType.SystemMessage,
                PayloadTypeName = this.serializer.GetTypeName(message),
                PayloadBytes = this.serializer.Serialize(message),
            };

            this.transport.Send(target, envelope).Subscribe(
                _ => { },
                ex => { });
        }

        private static GossipMemberState ToMemberState(Member member)
        {
            return new GossipMemberState
            {
                NodeId = member.Endpoint.NodeId,
                Port = member.Endpoint.Port,
                Status = (int)member.Status,
                HeartbeatSequence = member.HeartbeatSequence,
                LastSeenTicks = member.LastSeen.Ticks,
            };
        }
    }
}
