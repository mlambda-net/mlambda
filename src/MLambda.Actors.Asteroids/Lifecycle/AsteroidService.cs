// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AsteroidService.cs" company="MLambda">
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

namespace MLambda.Actors.Asteroids.Lifecycle
{
    using System;
    using System.Reactive.Linq;
    using System.Threading;
    using MLambda.Actors.Cluster.Abstraction;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Satellite.Abstraction;

    /// <summary>
    /// Lifecycle manager for asteroid nodes. Handles registration with
    /// all configured cluster nodes, periodic heartbeats, and graceful shutdown.
    /// Asteroids are lightweight gateway nodes that do not host actors.
    /// </summary>
    public class AsteroidService : IAsteroidService
    {
        private readonly ActorCatalogConfig config;
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;

        private Timer heartbeatTimer;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsteroidService"/> class.
        /// </summary>
        /// <param name="config">The actor catalog configuration.</param>
        /// <param name="transport">The transport layer.</param>
        /// <param name="serializer">The message serializer.</param>
        public AsteroidService(
            ActorCatalogConfig config,
            ITransport transport,
            IMessageSerializer serializer)
        {
            this.config = config;
            this.transport = transport;
            this.serializer = serializer;
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (this.config.ClusterNodes == null || this.config.ClusterNodes.Count == 0)
            {
                throw new InvalidOperationException(
                    "Asteroid node requires at least one cluster node in ClusterNodes configuration.");
            }

            this.RegisterWithClusterNodes();

            this.heartbeatTimer = new Timer(
                _ => this.SendHeartbeats(),
                null,
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(5));
        }

        /// <inheritdoc/>
        public void Stop()
        {
            this.heartbeatTimer?.Dispose();
            this.heartbeatTimer = null;

            this.SendDisconnectToClusterNodes();
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

        private void RegisterWithClusterNodes()
        {
            var registerMsg = new AsteroidRegister
            {
                AsteroidEndpoint = this.config.LocalEndpoint,
            };

            bool anyConnected = false;

            foreach (var clusterNode in this.config.ClusterNodes)
            {
                try
                {
                    var envelope = new Envelope
                    {
                        CorrelationId = Guid.NewGuid(),
                        TargetRoute = "route",
                        SourceNode = this.config.LocalEndpoint,
                        Type = EnvelopeType.Topology,
                        PayloadTypeName = this.serializer.GetTypeName(registerMsg),
                        PayloadBytes = this.serializer.Serialize(registerMsg),
                    };

                    this.transport.Send(clusterNode, envelope)
                        .Subscribe(_ => { }, ex => { });
                    anyConnected = true;
                }
                catch (Exception)
                {
                    // Continue to next cluster node.
                }
            }

            if (!anyConnected)
            {
                throw new InvalidOperationException(
                    "Asteroid could not connect to any cluster node.");
            }
        }

        private void SendHeartbeats()
        {
            var heartbeat = new AsteroidHeartbeat
            {
                AsteroidEndpoint = this.config.LocalEndpoint,
            };

            foreach (var clusterNode in this.config.ClusterNodes)
            {
                try
                {
                    var envelope = new Envelope
                    {
                        CorrelationId = Guid.NewGuid(),
                        TargetRoute = "route",
                        SourceNode = this.config.LocalEndpoint,
                        Type = EnvelopeType.Topology,
                        PayloadTypeName = this.serializer.GetTypeName(heartbeat),
                        PayloadBytes = this.serializer.Serialize(heartbeat),
                    };

                    this.transport.Send(clusterNode, envelope)
                        .Subscribe(_ => { }, ex => { });
                }
                catch (Exception)
                {
                    // Ignore heartbeat failures.
                }
            }
        }

        private void SendDisconnectToClusterNodes()
        {
            var disconnect = new AsteroidDisconnected
            {
                AsteroidEndpoint = this.config.LocalEndpoint,
            };

            foreach (var clusterNode in this.config.ClusterNodes)
            {
                try
                {
                    var envelope = new Envelope
                    {
                        CorrelationId = Guid.NewGuid(),
                        TargetRoute = "route",
                        SourceNode = this.config.LocalEndpoint,
                        Type = EnvelopeType.Topology,
                        PayloadTypeName = this.serializer.GetTypeName(disconnect),
                        PayloadBytes = this.serializer.Serialize(disconnect),
                    };

                    this.transport.Send(clusterNode, envelope)
                        .Subscribe(_ => { }, ex => { });
                }
                catch (Exception)
                {
                    // Ignore disconnect failures during shutdown.
                }
            }
        }
    }
}
