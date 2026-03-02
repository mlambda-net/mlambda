// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClusterManager.cs" company="MLambda">
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
    using System.Reactive.Subjects;
    using MLambda.Actors.Abstraction.Core;
    using MLambda.Actors.Gossip.Abstraction;
    using MLambda.Actors.Gossip.Messages;
    using MLambda.Actors.Network;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Manages cluster membership lifecycle using gossip protocol.
    /// </summary>
    public class ClusterManager : ICluster, IDisposable
    {
        private readonly ClusterConfig config;
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;
        private readonly GossipProtocol gossipProtocol;
        private readonly IFailureDetector failureDetector;
        private readonly IEventStream eventStream;
        private readonly Subject<ClusterEvent> eventSubject;

        private IDisposable heartbeatTimer;
        private IDisposable failureCheckTimer;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterManager"/> class.
        /// </summary>
        /// <param name="config">The cluster configuration.</param>
        /// <param name="transport">The transport layer.</param>
        /// <param name="serializer">The message serializer.</param>
        /// <param name="failureDetector">The failure detector.</param>
        /// <param name="eventStream">The event stream.</param>
        public ClusterManager(
            ClusterConfig config,
            ITransport transport,
            IMessageSerializer serializer,
            IFailureDetector failureDetector,
            IEventStream eventStream)
        {
            this.config = config;
            this.transport = transport;
            this.serializer = serializer;
            this.failureDetector = failureDetector;
            this.eventStream = eventStream;
            this.eventSubject = new Subject<ClusterEvent>();
            this.gossipProtocol = new GossipProtocol(config, transport, serializer);
            this.gossipProtocol.StateChanged += this.OnStateChanged;
        }

        /// <inheritdoc/>
        public NodeEndpoint Self => this.config.LocalEndpoint;

        /// <inheritdoc/>
        public IObservable<ClusterEvent> Events => this.eventSubject.AsObservable();

        /// <inheritdoc/>
        public IReadOnlyCollection<Member> Members =>
            this.gossipProtocol.CurrentState.Members.Values.ToList().AsReadOnly();

        /// <inheritdoc/>
        public Member GetMember(Guid nodeId)
        {
            this.gossipProtocol.CurrentState.Members.TryGetValue(nodeId, out var member);
            return member;
        }

        /// <summary>
        /// Starts the cluster manager and joins the cluster.
        /// </summary>
        public void Start()
        {
            var selfMember = new Member(this.config.LocalEndpoint, MemberStatus.Up)
            {
                HeartbeatSequence = 1,
            };
            this.gossipProtocol.UpdateMember(selfMember);

            this.gossipProtocol.Start();

            this.heartbeatTimer = Observable.Interval(this.config.HeartbeatInterval)
                .Subscribe(_ => this.IncrementHeartbeat());

            this.failureCheckTimer = Observable.Interval(this.config.HeartbeatInterval)
                .Subscribe(_ => this.CheckFailures());

            foreach (var seed in this.config.SeedNodes)
            {
                if (seed.NodeId != this.config.LocalEndpoint.NodeId)
                {
                    this.SendJoinRequest(seed);
                }
            }
        }

        /// <summary>
        /// Gracefully leaves the cluster.
        /// </summary>
        public void Leave()
        {
            var leave = new LeaveRequest
            {
                NodeId = this.config.LocalEndpoint.NodeId,
            };

            foreach (var seed in this.config.SeedNodes)
            {
                if (seed.NodeId != this.config.LocalEndpoint.NodeId)
                {
                    this.SendSystemMessage(seed, leave);
                }
            }

            this.Stop();
        }

        /// <summary>
        /// Stops the cluster manager.
        /// </summary>
        public void Stop()
        {
            this.heartbeatTimer?.Dispose();
            this.failureCheckTimer?.Dispose();
            this.gossipProtocol.Stop();
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
                this.gossipProtocol.Dispose();
                this.eventSubject.Dispose();
            }
        }

        private void IncrementHeartbeat()
        {
            var state = this.gossipProtocol.CurrentState;
            if (state.Members.TryGetValue(this.config.LocalEndpoint.NodeId, out var self))
            {
                self.HeartbeatSequence++;
                self.LastSeen = DateTimeOffset.UtcNow;
                this.gossipProtocol.UpdateMember(self);
            }
        }

        private void CheckFailures()
        {
            var state = this.gossipProtocol.CurrentState;
            var leader = this.GetLeader();

            foreach (var member in state.Members.Values.ToList())
            {
                if (member.Endpoint.NodeId == this.config.LocalEndpoint.NodeId)
                {
                    continue;
                }

                if (member.Status == MemberStatus.Up || member.Status == MemberStatus.Joining)
                {
                    this.failureDetector.Heartbeat(member.Endpoint.NodeId);
                }

                if (member.Status == MemberStatus.Up && !this.failureDetector.IsAvailable(member.Endpoint.NodeId))
                {
                    member.Status = MemberStatus.Suspect;
                    member.HeartbeatSequence++;
                    this.gossipProtocol.UpdateMember(member);
                    this.PublishEvent(new MemberSuspected(member));
                }

                if (member.Status == MemberStatus.Suspect)
                {
                    var elapsed = DateTimeOffset.UtcNow - member.LastSeen;
                    if (elapsed > this.config.SuspectTimeout)
                    {
                        member.Status = MemberStatus.Down;
                        member.HeartbeatSequence++;
                        this.gossipProtocol.UpdateMember(member);
                        this.PublishEvent(new MemberDown(member));
                    }
                }

                if (member.Status == MemberStatus.Joining && leader != null
                    && leader.NodeId == this.config.LocalEndpoint.NodeId)
                {
                    member.Status = MemberStatus.Up;
                    member.HeartbeatSequence++;
                    this.gossipProtocol.UpdateMember(member);
                    this.PublishEvent(new MemberUp(member));
                }
            }
        }

        private NodeEndpoint GetLeader()
        {
            var state = this.gossipProtocol.CurrentState;
            var upMembers = state.Members.Values
                .Where(m => m.Status == MemberStatus.Up)
                .OrderBy(m => m.Endpoint.NodeId)
                .FirstOrDefault();

            return upMembers?.Endpoint;
        }

        private void OnStateChanged(List<(Member Member, MemberStatus OldStatus)> changes)
        {
            foreach (var (member, oldStatus) in changes)
            {
                switch (member.Status)
                {
                    case MemberStatus.Joining:
                        this.PublishEvent(new MemberJoined(member));
                        this.failureDetector.Heartbeat(member.Endpoint.NodeId);
                        break;
                    case MemberStatus.Up:
                        this.PublishEvent(new MemberUp(member));
                        break;
                    case MemberStatus.Leaving:
                        this.PublishEvent(new MemberLeft(member));
                        break;
                    case MemberStatus.Suspect:
                        this.PublishEvent(new MemberSuspected(member));
                        break;
                    case MemberStatus.Down:
                        this.PublishEvent(new MemberDown(member));
                        break;
                    case MemberStatus.Removed:
                        this.PublishEvent(new MemberRemoved(member));
                        break;
                }
            }
        }

        private void PublishEvent(ClusterEvent evt)
        {
            this.eventSubject.OnNext(evt);
            this.eventStream.Publish(evt);
        }

        private void SendJoinRequest(NodeEndpoint target)
        {
            var join = new JoinRequest
            {
                NodeId = this.config.LocalEndpoint.NodeId,
                Host = this.config.LocalEndpoint.Host,
                Port = this.config.LocalEndpoint.Port,
            };

            this.SendSystemMessage(target, join);
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
    }
}
