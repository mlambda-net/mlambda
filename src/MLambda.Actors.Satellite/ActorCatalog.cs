// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActorCatalog.cs" company="MLambda">
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

namespace MLambda.Actors.Satellite
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Abstraction.Routing;
    using MLambda.Actors.Cluster;
    using MLambda.Actors.Cluster.Abstraction;
    using MLambda.Actors.Gossip;
    using MLambda.Actors.Gossip.Abstraction;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Satellite.Abstraction;
    using MLambda.Actors.Fortress;
    using MLambda.Actors.Satellite.Core;
    using MLambda.Actors.Satellite.Resolver;
    using MLambda.Actors.Satellite.Lifecycle;
    using MLambda.Actors.Satellite.Worker;

    /// <summary>
    /// A fully configured remote actor node that manages the complete lifecycle
    /// of transport, cluster, dispatcher, and actor resolver services.
    /// Branches behavior based on the configured <see cref="NodeType"/>.
    /// </summary>
    public class ActorCatalog : IActorCatalog
    {
        private readonly ServiceProvider provider;
        private readonly ActorCatalogConfig config;

        private ITransport transport;
        private ClusterManager cluster;
        private IRemoteMessageDispatcher dispatcher;
        private IClusterService clusterService;
        private ISatelliteService satelliteService;
        private FortressService fortressService;
        private FortressClientService fortressClientService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorCatalog"/> class.
        /// </summary>
        /// <param name="provider">The built service provider.</param>
        public ActorCatalog(ServiceProvider provider)
        {
            this.provider = provider;
            this.config = provider.GetRequiredService<ActorCatalogConfig>();
        }

        /// <summary>
        /// Builds a fully configured remote actor node from the given configuration.
        /// </summary>
        /// <param name="configure">The configuration action.</param>
        /// <returns>A fully configured <see cref="IActorCatalog"/>.</returns>
        public static IActorCatalog Build(Action<ActorCatalogConfig> configure)
        {
            var services = new ServiceCollection();
            return services.BuildSatelliteNode(configure);
        }

        /// <inheritdoc/>
        public IServiceProvider Services => this.provider;

        /// <inheritdoc/>
        public IUserContext User => this.provider.GetRequiredService<IUserContext>();

        /// <inheritdoc/>
        public async Task Start()
        {
            // Transport (all node types).
            this.transport = this.provider.GetRequiredService<ITransport>();
            await this.transport.Start();

            // Remote message dispatcher (all node types).
            this.dispatcher = this.provider.GetRequiredService<IRemoteMessageDispatcher>();
            this.dispatcher.Start();

            var isCluster = this.config.NodeType == NodeType.Cluster
                || this.config.NodeType == NodeType.Hybrid;
            var isSatellite = this.config.NodeType == NodeType.Satellite
                || this.config.NodeType == NodeType.Hybrid;

            // Cluster services: gossip + cluster actors (RouteActor, StateActor, DeliveryActor).
            if (isCluster)
            {
                this.cluster = this.provider.GetRequiredService<ICluster>() as ClusterManager;
                this.cluster?.Start();

                this.clusterService = this.provider.GetRequiredService<IClusterService>();
                this.clusterService.Start();

                // Register cluster system actors in the resolver so that
                // RemoteMessageDispatcher can route topology messages to them.
                if (this.clusterService is ClusterService cs)
                {
                    var resolver = this.provider.GetRequiredService<IActorResolver>();
                    foreach (var kvp in cs.SystemActors)
                    {
                        resolver.Register(kvp.Key, kvp.Value);
                    }
                }
            }

            // Fortress security (cluster-side: CA actors + self-cert).
            this.fortressService = this.provider.GetService<FortressService>();
            if (this.fortressService != null)
            {
                this.fortressService.Start();

                var resolver = this.provider.GetRequiredService<IActorResolver>();
                foreach (var kvp in this.fortressService.SystemActors)
                {
                    resolver.Register(kvp.Key, kvp.Value);
                }
            }

            // Fortress security (satellite-side: request cert from cluster).
            this.fortressClientService = this.provider.GetService<FortressClientService>();
            this.fortressClientService?.Start();

            // Satellite services: worker actor + resolver + registration.
            if (isSatellite)
            {
                var systemContext = this.provider.GetRequiredService<ISystemContext>();

                if (this.config.NodeType == NodeType.Satellite)
                {
                    // Pure satellite: use SatelliteService for full lifecycle
                    // (spawn actors + register with cluster nodes over network).
                    this.satelliteService = this.provider.GetRequiredService<ISatelliteService>();
                    this.satelliteService.Start();
                }
                else
                {
                    // Hybrid: spawn worker/resolver actors directly and register
                    // them in the resolver, then self-register with local RouteActor.
                    var resolver = this.provider.GetRequiredService<IActorResolver>();

                    var resolverAddr = systemContext.Spawn<ActorResolverActor>().Wait();
                    resolver.Register("resolver", resolverAddr);

                    var workerAddr = systemContext.Spawn<WorkerActor>().Wait();
                    resolver.Register("worker", workerAddr);

                    this.SelfRegisterAsSatellite();
                }
            }
        }

        /// <inheritdoc/>
        public async Task Stop()
        {
            // Stop fortress services.
            this.fortressService?.Stop();
            this.fortressClientService?.Stop();

            // Stop satellite services.
            this.satelliteService?.Stop();

            // Stop cluster services.
            this.clusterService?.Stop();
            this.dispatcher?.Stop();
            this.cluster?.Leave();

            if (this.transport != null)
            {
                await this.transport.Stop();
            }
        }

        /// <inheritdoc/>
        public async Task<IAddress> Spawn<T>()
            where T : IActor =>
            await this.User.Spawn<T>();

        /// <inheritdoc/>
        public IAddress For<T>()
            where T : IActor
        {
            var route = typeof(T).GetCustomAttribute<RouteAttribute>()?.Name
                ?? throw new InvalidOperationException(
                    $"Actor type {typeof(T).Name} does not have a [Route] attribute.");

            // Determine the cluster target for routing.
            // Cluster/Hybrid nodes route through themselves.
            // Satellite nodes route through the first cluster node.
            var isCluster = this.config.NodeType == NodeType.Cluster
                || this.config.NodeType == NodeType.Hybrid;
            var clusterTarget = isCluster
                ? this.config.LocalEndpoint
                : this.config.ClusterNodes?.FirstOrDefault();

            if (clusterTarget == null)
            {
                throw new InvalidOperationException(
                    "No cluster target available for routing. " +
                    "Satellite nodes require at least one cluster node in ClusterNodes.");
            }

            return new RouteAddress(
                route,
                this.provider.GetRequiredService<IActorResolver>(),
                this.provider.GetRequiredService<ITransport>(),
                this.provider.GetRequiredService<IMessageSerializer>(),
                this.provider.GetRequiredService<NodeEndpoint>(),
                clusterTarget,
                this.provider.GetRequiredService<ConcurrentDictionary<Guid, TaskCompletionSource<object>>>());
        }

        /// <inheritdoc/>
        public IAddress For<T>(Parameter parameters)
            where T : IActor
        {
            var template = typeof(T).GetCustomAttribute<RouteAttribute>()?.Name
                ?? throw new InvalidOperationException(
                    $"Actor type {typeof(T).Name} does not have a [Route] attribute.");

            var routeTemplate = new RouteTemplate(template);
            var resolvedRoute = routeTemplate.IsParameterized && parameters != null && !parameters.IsEmpty
                ? routeTemplate.Resolve(parameters)
                : template;

            // Determine the cluster target for routing.
            var isCluster = this.config.NodeType == NodeType.Cluster
                || this.config.NodeType == NodeType.Hybrid;
            var clusterTarget = isCluster
                ? this.config.LocalEndpoint
                : this.config.ClusterNodes?.FirstOrDefault();

            if (clusterTarget == null)
            {
                throw new InvalidOperationException(
                    "No cluster target available for routing. " +
                    "Satellite nodes require at least one cluster node in ClusterNodes.");
            }

            return new RouteAddress(
                resolvedRoute,
                this.provider.GetRequiredService<IActorResolver>(),
                this.provider.GetRequiredService<ITransport>(),
                this.provider.GetRequiredService<IMessageSerializer>(),
                this.provider.GetRequiredService<NodeEndpoint>(),
                clusterTarget,
                this.provider.GetRequiredService<ConcurrentDictionary<Guid, TaskCompletionSource<object>>>());
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Stop().GetAwaiter().GetResult();
            this.provider?.Dispose();
        }

        /// <summary>
        /// For Hybrid nodes, registers this node as a satellite with its own
        /// local RouteActor, making its routes available for dispatch.
        /// </summary>
        private void SelfRegisterAsSatellite()
        {
            var typeRegistry = this.provider.GetRequiredService<ActorTypeRegistry>();
            var capabilities = typeRegistry.GetAllRoutes().ToList();
            var serializer = this.provider.GetRequiredService<IMessageSerializer>();

            var registerMsg = new SatelliteRegister
            {
                SatelliteEndpoint = this.config.LocalEndpoint,
                Capabilities = capabilities,
            };

            var envelope = new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetRoute = "route",
                SourceNode = this.config.LocalEndpoint,
                Type = EnvelopeType.Topology,
                PayloadTypeName = serializer.GetTypeName(registerMsg),
                PayloadBytes = serializer.Serialize(registerMsg),
            };

            this.transport.Send(this.config.LocalEndpoint, envelope)
                .Subscribe(_ => { }, ex => { });
        }
    }
}
