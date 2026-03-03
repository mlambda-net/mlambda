// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RouteActor.cs" company="MLambda">
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

namespace MLambda.Actors.Cluster
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reactive;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Cluster.Abstraction;
    using MLambda.Actors.Gossip.Data;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Cluster-side system actor that manages actor routing and satellite tracking.
    /// Uses a gossip-replicated GTree for the route table.
    /// </summary>
    [Route("route")]
    public class RouteActor : Actor
    {
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;
        private readonly GossipDataReplicator replicator;
        private readonly GTree<string, RouteInfo> routeTable;
        private readonly List<SatelliteInfo> satellites;
        private readonly ConcurrentDictionary<string, List<DispatchWork>> pendingCreations;
        private int roundRobinIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteActor"/> class.
        /// </summary>
        /// <param name="transport">The transport layer.</param>
        /// <param name="serializer">The message serializer.</param>
        /// <param name="replicator">The CRDT replicator for gossip sync.</param>
        public RouteActor(
            ITransport transport,
            IMessageSerializer serializer,
            GossipDataReplicator replicator)
        {
            this.transport = transport;
            this.serializer = serializer;
            this.replicator = replicator;
            this.routeTable = new GTree<string, RouteInfo>(
                "cluster-route-table",
                transport.LocalEndpoint?.NodeId ?? "local");
            this.satellites = new List<SatelliteInfo>();
            this.pendingCreations = new ConcurrentDictionary<string, List<DispatchWork>>(
                StringComparer.OrdinalIgnoreCase);
            this.roundRobinIndex = 0;

            this.replicator.Register("cluster-route-table", this.routeTable);
        }

        /// <inheritdoc/>
        protected override Behavior Receive(object data)
            => data switch
            {
                SatelliteRegister msg => Actor.Behavior<SatelliteRegistered, SatelliteRegister>(
                    this.HandleSatelliteRegister, msg),
                SatelliteHeartbeat msg => Actor.Behavior<Unit, SatelliteHeartbeat>(
                    this.HandleSatelliteHeartbeat, msg),
                SatelliteDisconnected msg => Actor.Behavior<Unit, SatelliteDisconnected>(
                    this.HandleSatelliteDisconnected, msg),
                DispatchWork msg => Actor.Behavior<WorkResult, DispatchWork>(
                    this.HandleDispatchWork, msg),
                ActorCreateResult msg => Actor.Behavior<Unit, ActorCreateResult>(
                    this.HandleActorCreateResult, msg),
                _ => Actor.Ignore,
            };

        private IObservable<SatelliteRegistered> HandleSatelliteRegister(SatelliteRegister msg)
        {
            var existing = this.satellites.Find(s => s.Endpoint == msg.SatelliteEndpoint);
            if (existing != null)
            {
                existing.Capabilities = msg.Capabilities;
            }
            else
            {
                this.satellites.Add(new SatelliteInfo
                {
                    Endpoint = msg.SatelliteEndpoint,
                    Capabilities = msg.Capabilities,
                    Load = 0,
                });
            }

            return Observable.Return(new SatelliteRegistered { Success = true });
        }

        private IObservable<Unit> HandleSatelliteHeartbeat(SatelliteHeartbeat msg)
        {
            var sat = this.satellites.Find(s => s.Endpoint == msg.SatelliteEndpoint);
            if (sat != null)
            {
                sat.Load = msg.Load;
            }

            return Actor.Done;
        }

        private IObservable<Unit> HandleSatelliteDisconnected(SatelliteDisconnected msg)
        {
            this.satellites.RemoveAll(s => s.Endpoint == msg.SatelliteEndpoint);

            // Mark all routes hosted on this satellite as Dead.
            foreach (var kvp in this.routeTable.GetOrdered())
            {
                if (kvp.Value.Satellite == msg.SatelliteEndpoint)
                {
                    this.routeTable.Set(kvp.Key, new RouteInfo
                    {
                        Route = kvp.Value.Route,
                        Satellite = kvp.Value.Satellite,
                        Status = RouteStatus.Dead,
                        LastUpdated = DateTimeOffset.UtcNow.Ticks,
                    });
                }
            }

            return Actor.Done;
        }

        private IObservable<WorkResult> HandleDispatchWork(DispatchWork msg)
        {
            if (this.routeTable.TryGet(msg.TargetRoute, out var routeInfo)
                && routeInfo.Status == RouteStatus.Running)
            {
                // Route exists and is running: persist message then request delivery.
                var persistMsg = new PersistMessage
                {
                    MessageId = Guid.NewGuid(),
                    Route = msg.TargetRoute,
                    PayloadTypeName = msg.PayloadTypeName,
                    PayloadBytes = msg.PayloadBytes,
                    IsAsk = msg.IsAsk,
                    CorrelationId = msg.CorrelationId,
                    OriginNode = msg.OriginNode,
                    PersistedAt = DateTimeOffset.UtcNow,
                };

                this.SendLocalMessage("state", persistMsg);

                var deliveryReq = new DeliveryRequest
                {
                    Route = msg.TargetRoute,
                    Satellite = routeInfo.Satellite,
                };

                this.SendLocalMessage("delivery", deliveryReq);

                return Observable.Return(new WorkResult
                {
                    CorrelationId = msg.CorrelationId,
                    Success = true,
                });
            }
            else
            {
                // Route not found or not running: create actor on a satellite.
                if (this.satellites.Count == 0)
                {
                    return Observable.Return(new WorkResult
                    {
                        CorrelationId = msg.CorrelationId,
                        Success = false,
                        ErrorMessage = "No satellites available.",
                    });
                }

                // Queue the message for delivery after creation.
                this.pendingCreations.AddOrUpdate(
                    msg.TargetRoute,
                    _ => new List<DispatchWork> { msg },
                    (_, list) =>
                    {
                        list.Add(msg);
                        return list;
                    });

                // Check if we already have a pending creation for this route.
                if (!this.routeTable.TryGet(msg.TargetRoute, out var existing)
                    || existing.Status != RouteStatus.Waiting)
                {
                    // Round-robin satellite selection.
                    var satellite = this.satellites[this.roundRobinIndex % this.satellites.Count];
                    this.roundRobinIndex++;

                    // Mark route as Waiting.
                    this.routeTable.Set(msg.TargetRoute, new RouteInfo
                    {
                        Route = msg.TargetRoute,
                        Satellite = satellite.Endpoint,
                        Status = RouteStatus.Waiting,
                        LastUpdated = DateTimeOffset.UtcNow.Ticks,
                    });

                    // Send create request to satellite.
                    var createMsg = new ActorCreate
                    {
                        CorrelationId = Guid.NewGuid(),
                        TargetRoute = msg.TargetRoute,
                        OriginNode = this.transport.LocalEndpoint,
                    };

                    this.SendToSatellite(satellite.Endpoint, createMsg);
                }

                return Observable.Return(new WorkResult
                {
                    CorrelationId = msg.CorrelationId,
                    Success = true,
                });
            }
        }

        private IObservable<Unit> HandleActorCreateResult(ActorCreateResult msg)
        {
            if (msg.Success)
            {
                // Update route to Running.
                if (this.routeTable.TryGet(msg.TargetRoute, out var info))
                {
                    this.routeTable.Set(msg.TargetRoute, new RouteInfo
                    {
                        Route = msg.TargetRoute,
                        Satellite = info.Satellite,
                        Status = RouteStatus.Running,
                        LastUpdated = DateTimeOffset.UtcNow.Ticks,
                    });
                }

                // Flush pending messages.
                if (this.pendingCreations.TryRemove(msg.TargetRoute, out var pending))
                {
                    foreach (var work in pending)
                    {
                        var persistMsg = new PersistMessage
                        {
                            MessageId = Guid.NewGuid(),
                            Route = work.TargetRoute,
                            PayloadTypeName = work.PayloadTypeName,
                            PayloadBytes = work.PayloadBytes,
                            IsAsk = work.IsAsk,
                            CorrelationId = work.CorrelationId,
                            OriginNode = work.OriginNode,
                            PersistedAt = DateTimeOffset.UtcNow,
                        };

                        this.SendLocalMessage("state", persistMsg);
                    }

                    if (info != null)
                    {
                        this.SendLocalMessage("delivery", new DeliveryRequest
                        {
                            Route = msg.TargetRoute,
                            Satellite = info.Satellite,
                        });
                    }
                }
            }
            else
            {
                // Mark route as Dead.
                this.routeTable.Set(msg.TargetRoute, new RouteInfo
                {
                    Route = msg.TargetRoute,
                    Status = RouteStatus.Dead,
                    LastUpdated = DateTimeOffset.UtcNow.Ticks,
                });

                this.pendingCreations.TryRemove(msg.TargetRoute, out _);
            }

            return Actor.Done;
        }

        private void SendLocalMessage(string targetRoute, object message)
        {
            var envelope = new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetRoute = targetRoute,
                SourceNode = this.transport.LocalEndpoint,
                Type = EnvelopeType.Topology,
                PayloadTypeName = this.serializer.GetTypeName(message),
                PayloadBytes = this.serializer.Serialize(message),
            };

            this.transport.Send(this.transport.LocalEndpoint, envelope)
                .Subscribe(_ => { }, ex => { });
        }

        private void SendToSatellite(NodeEndpoint satellite, object message)
        {
            var envelope = new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetRoute = "worker",
                SourceNode = this.transport.LocalEndpoint,
                Type = EnvelopeType.Topology,
                PayloadTypeName = this.serializer.GetTypeName(message),
                PayloadBytes = this.serializer.Serialize(message),
            };

            this.transport.Send(satellite, envelope)
                .Subscribe(_ => { }, ex => { });
        }

        /// <summary>
        /// Tracks satellite node information.
        /// </summary>
        private class SatelliteInfo
        {
            /// <summary>
            /// Gets or sets the satellite endpoint.
            /// </summary>
            public NodeEndpoint Endpoint { get; set; }

            /// <summary>
            /// Gets or sets the capabilities this satellite can host.
            /// </summary>
            public List<string> Capabilities { get; set; }

            /// <summary>
            /// Gets or sets the current load.
            /// </summary>
            public int Load { get; set; }
        }
    }
}
