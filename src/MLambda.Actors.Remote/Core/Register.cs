// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Register.cs" company="MLambda">
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

namespace MLambda.Actors.Remote.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using MLambda.Actors.Abstraction.Core;
    using MLambda.Actors.Cluster;
    using MLambda.Actors.Cluster.Core;
    using MLambda.Actors.Core;
    using MLambda.Actors.Gossip;
    using MLambda.Actors.Gossip.Abstraction;
    using MLambda.Actors.Monitoring.Core;
    using MLambda.Actors.Network;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Remote.Abstraction;
    using MLambda.Actors.Remote.Resolver;
    using MLambda.Actors.Remote.Satellite;
    using MLambda.Actors.Remote.Worker;

    /// <summary>
    /// Dependency injection registration for remote actor services.
    /// </summary>
    public static class Register
    {
        /// <summary>
        /// Adds remote actor services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddRemoteActor(this IServiceCollection services)
        {
            services.AddSingleton<IActorRegistry, ActorRegistry>();
            services.AddSingleton<ConcurrentDictionary<Guid, TaskCompletionSource<object>>>();
            services.AddSingleton<IAddressResolver, AddressResolver>();
            services.AddSingleton<IRemoteMessageDispatcher, RemoteMessageDispatcher>();
            return services;
        }

        /// <summary>
        /// Configures a complete remote actor node with all required services:
        /// actor system, network transport, gossip cluster, remote messaging,
        /// cluster actors, and monitoring. Branches service registration by node type.
        /// Returns a built <see cref="IActorAddress"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>A fully configured <see cref="IActorAddress"/>.</returns>
        public static IActorAddress BuildRemoteNode(
            this IServiceCollection services,
            Action<ActorAddressConfig> configure)
        {
            var config = new ActorAddressConfig();
            configure(config);

            TopologyValidator.Validate(config);

            var localEndpoint = config.LocalEndpoint;
            var isCluster = config.NodeType == NodeType.Cluster || config.NodeType == NodeType.Hybrid;
            var isSatellite = config.NodeType == NodeType.Satellite || config.NodeType == NodeType.Hybrid;

            // Core actor system (all node types).
            services.AddActor();
            services.AddSingleton(config);
            services.AddSingleton(localEndpoint);
            services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
            services.AddSingleton<ITransport>(provider =>
                new TcpTransport(
                    provider.GetRequiredService<NodeEndpoint>(),
                    provider.GetRequiredService<IEventStream>()));

            // Actor type registry and resolver (all node types).
            // Cluster-only nodes get an empty registry, but the resolver
            // is still needed by RemoteMessageDispatcher.
            var typeRegistry = new ActorTypeRegistry(isSatellite ? config.ActorTypes : Array.Empty<Type>());
            services.AddSingleton(typeRegistry);
            services.AddSingleton<IActorResolver, ActorResolverService>();

            // Remote messaging (all node types).
            services.AddRemoteActor();

            // Cluster services: gossip, cluster actors.
            if (isCluster)
            {
                var clusterConfig = new ClusterConfig
                {
                    LocalEndpoint = localEndpoint,
                    SeedNodes = config.SeedNodes,
                    GossipInterval = config.GossipInterval,
                    HeartbeatInterval = config.HeartbeatInterval,
                    PhiThreshold = config.PhiThreshold,
                    SuspectTimeout = config.SuspectTimeout,
                };
                services.AddSingleton(clusterConfig);
                services.AddSingleton<IFailureDetector>(
                    new PhiAccrualFailureDetector(clusterConfig.PhiThreshold));
                services.AddSingleton<ICluster>(provider =>
                    new ClusterManager(
                        provider.GetRequiredService<ClusterConfig>(),
                        provider.GetRequiredService<ITransport>(),
                        provider.GetRequiredService<IMessageSerializer>(),
                        provider.GetRequiredService<IFailureDetector>(),
                        provider.GetRequiredService<IEventStream>()));

                services.AddCluster();
            }

            // Satellite/Worker services: user actor types, worker.
            if (isSatellite)
            {
                foreach (var actorType in config.ActorTypes)
                {
                    services.AddTransient(actorType);
                }

                services.AddTransient<ActorResolverActor>();
                services.AddTransient<WorkerActor>();

                // Pure satellite nodes use SatelliteService for cluster registration.
                // Hybrid nodes self-register locally via ActorAddress.
                if (config.NodeType == NodeType.Satellite)
                {
                    services.AddSingleton<ISatelliteService, SatelliteService>();
                }
            }

            if (config.EnableMonitoring)
            {
                services.AddActorMonitoring(m =>
                {
                    m.NodeId = config.NodeId;
                    m.OtlpEndpoint = config.OtlpEndpoint;
                });
            }

            var provider = services.BuildServiceProvider();
            return new ActorAddress(provider);
        }
    }
}
