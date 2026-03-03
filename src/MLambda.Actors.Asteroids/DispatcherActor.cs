// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DispatcherActor.cs" company="MLambda">
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

namespace MLambda.Actors.Asteroids
{
    using System;
    using System.Collections.Generic;
    using System.Reactive;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Cluster.Abstraction;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Satellite.Abstraction;

    /// <summary>
    /// Local actor on asteroid nodes that dispatches work to cluster nodes
    /// using round-robin selection. Receives <see cref="DispatchWork"/> messages
    /// from <see cref="AsteroidRouteAddress"/> and forwards them to a cluster
    /// node's RouteActor for processing.
    /// </summary>
    [Route("dispatcher")]
    public class DispatcherActor : Actor
    {
        private readonly ActorCatalogConfig config;
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;

        private List<NodeEndpoint> clusterNodes;
        private int roundRobinIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="DispatcherActor"/> class.
        /// </summary>
        /// <param name="config">The actor catalog configuration.</param>
        /// <param name="transport">The transport layer.</param>
        /// <param name="serializer">The message serializer.</param>
        public DispatcherActor(
            ActorCatalogConfig config,
            ITransport transport,
            IMessageSerializer serializer)
        {
            this.config = config;
            this.transport = transport;
            this.serializer = serializer;
            this.clusterNodes = new List<NodeEndpoint>(config.ClusterNodes ?? new List<NodeEndpoint>());
            this.roundRobinIndex = 0;
        }

        /// <inheritdoc/>
        protected override Behavior Receive(object data)
            => data switch
            {
                DispatchWork msg => Actor.Behavior<WorkResult, DispatchWork>(
                    this.HandleDispatchWork, msg),
                ClusterTopologyUpdate msg => Actor.Behavior<Unit, ClusterTopologyUpdate>(
                    this.HandleTopologyUpdate, msg),
                _ => Actor.Ignore,
            };

        private IObservable<WorkResult> HandleDispatchWork(DispatchWork msg)
        {
            if (this.clusterNodes.Count == 0)
            {
                return Observable.Return(new WorkResult
                {
                    CorrelationId = msg.CorrelationId,
                    Success = false,
                    ErrorMessage = "No cluster nodes available for dispatching.",
                });
            }

            var targetNode = this.SelectClusterNode();

            var envelope = new Envelope
            {
                CorrelationId = msg.CorrelationId,
                TargetRoute = "route",
                SourceNode = this.config.LocalEndpoint,
                Type = EnvelopeType.Topology,
                PayloadTypeName = this.serializer.GetTypeName(msg),
                PayloadBytes = this.serializer.Serialize(msg),
            };

            this.transport.Send(targetNode, envelope)
                .Subscribe(_ => { }, ex => { });

            return Observable.Return(new WorkResult
            {
                CorrelationId = msg.CorrelationId,
                Success = true,
            });
        }

        private IObservable<Unit> HandleTopologyUpdate(ClusterTopologyUpdate msg)
        {
            if (msg.ClusterNodes != null && msg.ClusterNodes.Count > 0)
            {
                this.clusterNodes = new List<NodeEndpoint>(msg.ClusterNodes);
                this.roundRobinIndex = 0;
            }

            return Actor.Done;
        }

        private NodeEndpoint SelectClusterNode()
        {
            var index = this.roundRobinIndex % this.clusterNodes.Count;
            this.roundRobinIndex = (this.roundRobinIndex + 1) % this.clusterNodes.Count;
            return this.clusterNodes[index];
        }
    }
}
