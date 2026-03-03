// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GossipDataReplicator.cs" company="MLambda">
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

namespace MLambda.Actors.Gossip.Data
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using MLambda.Actors.Gossip.Abstraction;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Replication service for CRDT data structures using the existing gossip transport.
    /// Periodically sends digests to random cluster peers and merges incoming state.
    /// </summary>
    public class GossipDataReplicator : IDisposable
    {
        private readonly ITransport transport;
        private readonly ICluster cluster;
        private readonly IMessageSerializer serializer;
        private readonly ConcurrentDictionary<string, RegisteredCrdt> registry;
        private readonly object syncLock = new object();
        private readonly Random random = new Random();
        private readonly TimeSpan syncInterval;

        private IDisposable syncTimer;
        private IDisposable messageSubscription;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="GossipDataReplicator"/> class.
        /// </summary>
        /// <param name="transport">The transport layer for sending and receiving messages.</param>
        /// <param name="cluster">The cluster membership provider.</param>
        /// <param name="serializer">The message serializer.</param>
        public GossipDataReplicator(ITransport transport, ICluster cluster, IMessageSerializer serializer)
            : this(transport, cluster, serializer, TimeSpan.FromSeconds(2))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GossipDataReplicator"/> class.
        /// </summary>
        /// <param name="transport">The transport layer for sending and receiving messages.</param>
        /// <param name="cluster">The cluster membership provider.</param>
        /// <param name="serializer">The message serializer.</param>
        /// <param name="syncInterval">The interval between sync rounds.</param>
        public GossipDataReplicator(
            ITransport transport,
            ICluster cluster,
            IMessageSerializer serializer,
            TimeSpan syncInterval)
        {
            this.transport = transport;
            this.cluster = cluster;
            this.serializer = serializer;
            this.syncInterval = syncInterval;
            this.registry = new ConcurrentDictionary<string, RegisteredCrdt>();
        }

        /// <summary>
        /// Registers a CRDT state instance for gossip replication.
        /// </summary>
        /// <typeparam name="T">The CRDT state type.</typeparam>
        /// <param name="stateId">A unique identifier for this state instance.</param>
        /// <param name="state">The CRDT state instance.</param>
        public void Register<T>(string stateId, T state)
            where T : class, ICrdtState<T>
        {
            var entry = new RegisteredCrdt
            {
                StateId = stateId,
                StateType = typeof(T),
                GetState = () => state,
                GetDigest = () => state.GetDigest(),
                SerializeState = () => this.serializer.Serialize(state),
                MergeState = remoteBytes =>
                {
                    var typeName = this.serializer.GetTypeName(state);
                    var remote = (T)this.serializer.Deserialize(remoteBytes, typeName);
                    var merged = state.Merge(remote);

                    // Update the registration with the merged state.
                    if (this.registry.TryGetValue(stateId, out var reg))
                    {
                        reg.GetState = () => merged;
                        reg.GetDigest = () => merged.GetDigest();
                        reg.SerializeState = () => this.serializer.Serialize(merged);
                        reg.MergeState = reg.MergeState; // Keep closure updated.
                    }
                },
            };

            this.registry[stateId] = entry;
        }

        /// <summary>
        /// Starts the replication service. Subscribes to incoming CrdtSync messages
        /// and begins periodic sync rounds.
        /// </summary>
        public void Start()
        {
            this.messageSubscription = this.transport.IncomingMessages
                .Where(e => e.Type == EnvelopeType.CrdtSync)
                .Subscribe(this.HandleIncomingMessage);

            this.syncTimer = Observable.Interval(this.syncInterval)
                .Subscribe(_ => this.SyncRound());
        }

        /// <summary>
        /// Stops the replication service.
        /// </summary>
        public void Stop()
        {
            this.syncTimer?.Dispose();
            this.syncTimer = null;
            this.messageSubscription?.Dispose();
            this.messageSubscription = null;
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

        private void SyncRound()
        {
            var peers = this.cluster.Members
                .Where(m => m.Endpoint != this.cluster.Self
                    && m.Status == MemberStatus.Up)
                .ToList();

            if (peers.Count == 0)
            {
                return;
            }

            // Select a random peer for this sync round.
            var target = peers[this.random.Next(peers.Count)];

            var digests = new List<CrdtDigest>();
            foreach (var kvp in this.registry)
            {
                try
                {
                    digests.Add(kvp.Value.GetDigest());
                }
                catch (Exception)
                {
                    // Skip states that fail to produce digests.
                }
            }

            if (digests.Count == 0)
            {
                return;
            }

            var request = new CrdtSyncRequest
            {
                SenderId = this.cluster.Self?.NodeId ?? "unknown",
                Digests = digests,
            };

            this.SendCrdtMessage(target.Endpoint, request);
        }

        private void HandleIncomingMessage(Envelope envelope)
        {
            try
            {
                var payload = this.serializer.Deserialize(
                    envelope.PayloadBytes, envelope.PayloadTypeName);

                switch (payload)
                {
                    case CrdtSyncRequest request:
                        this.HandleSyncRequest(request, envelope.SourceNode);
                        break;
                    case CrdtSyncResponse response:
                        this.HandleSyncResponse(response, envelope.SourceNode);
                        break;
                }
            }
            catch (Exception)
            {
                // Ignore deserialization errors.
            }
        }

        private void HandleSyncRequest(CrdtSyncRequest request, NodeEndpoint source)
        {
            var updatedStates = new List<CrdtStatePayload>();
            var requestedDigests = new List<CrdtDigest>();

            lock (this.syncLock)
            {
                foreach (var remoteDigest in request.Digests)
                {
                    if (this.registry.TryGetValue(remoteDigest.StateId, out var local))
                    {
                        var localDigest = local.GetDigest();
                        if (localDigest.Version > remoteDigest.Version)
                        {
                            // We have newer state; send it.
                            updatedStates.Add(new CrdtStatePayload
                            {
                                StateId = remoteDigest.StateId,
                                TypeName = this.serializer.GetTypeName(local.GetState()),
                                Data = local.SerializeState(),
                            });
                        }
                        else if (localDigest.Version < remoteDigest.Version)
                        {
                            // Remote has newer state; request it.
                            requestedDigests.Add(remoteDigest);
                        }
                    }
                    else
                    {
                        // We don't have this state; request it.
                        requestedDigests.Add(remoteDigest);
                    }
                }

                // Send states we have that the remote doesn't know about.
                foreach (var kvp in this.registry)
                {
                    if (!request.Digests.Any(d => d.StateId == kvp.Key))
                    {
                        updatedStates.Add(new CrdtStatePayload
                        {
                            StateId = kvp.Key,
                            TypeName = this.serializer.GetTypeName(kvp.Value.GetState()),
                            Data = kvp.Value.SerializeState(),
                        });
                    }
                }
            }

            var response = new CrdtSyncResponse
            {
                SenderId = this.cluster.Self?.NodeId ?? "unknown",
                States = updatedStates,
                RequestedDigests = requestedDigests,
            };

            this.SendCrdtMessage(source, response);
        }

        private void HandleSyncResponse(CrdtSyncResponse response, NodeEndpoint source)
        {
            lock (this.syncLock)
            {
                // Merge incoming states.
                foreach (var statePayload in response.States)
                {
                    if (this.registry.TryGetValue(statePayload.StateId, out var local))
                    {
                        try
                        {
                            local.MergeState(statePayload.Data);
                        }
                        catch (Exception)
                        {
                            // Ignore merge failures.
                        }
                    }
                }

                // Send back states that were requested.
                if (response.RequestedDigests.Count > 0)
                {
                    var states = new List<CrdtStatePayload>();
                    foreach (var digest in response.RequestedDigests)
                    {
                        if (this.registry.TryGetValue(digest.StateId, out var local))
                        {
                            states.Add(new CrdtStatePayload
                            {
                                StateId = digest.StateId,
                                TypeName = this.serializer.GetTypeName(local.GetState()),
                                Data = local.SerializeState(),
                            });
                        }
                    }

                    if (states.Count > 0)
                    {
                        var followUp = new CrdtSyncResponse
                        {
                            SenderId = this.cluster.Self?.NodeId ?? "unknown",
                            States = states,
                            RequestedDigests = new List<CrdtDigest>(),
                        };

                        this.SendCrdtMessage(source, followUp);
                    }
                }
            }
        }

        private void SendCrdtMessage(NodeEndpoint target, object message)
        {
            var envelope = new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetActorId = Guid.Empty,
                SourceActorId = Guid.Empty,
                SourceNode = this.transport.LocalEndpoint,
                Type = EnvelopeType.CrdtSync,
                PayloadTypeName = this.serializer.GetTypeName(message),
                PayloadBytes = this.serializer.Serialize(message),
            };

            this.transport.Send(target, envelope).Subscribe(
                _ => { },
                ex => { });
        }

        /// <summary>
        /// Internal registration entry for a CRDT state.
        /// </summary>
        private class RegisteredCrdt
        {
            /// <summary>
            /// Gets or sets the state identifier.
            /// </summary>
            public string StateId { get; set; }

            /// <summary>
            /// Gets or sets the CLR type of the state.
            /// </summary>
            public Type StateType { get; set; }

            /// <summary>
            /// Gets or sets a delegate that returns the current state instance.
            /// </summary>
            public Func<object> GetState { get; set; }

            /// <summary>
            /// Gets or sets a delegate that returns the current digest.
            /// </summary>
            public Func<CrdtDigest> GetDigest { get; set; }

            /// <summary>
            /// Gets or sets a delegate that serializes the current state.
            /// </summary>
            public Func<byte[]> SerializeState { get; set; }

            /// <summary>
            /// Gets or sets a delegate that merges remote state bytes into the local state.
            /// </summary>
            public Action<byte[]> MergeState { get; set; }
        }
    }
}
