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

namespace MLambda.Actors.Asteroids.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using MLambda.Actors.Abstraction.Core;
    using MLambda.Actors.Asteroids.Lifecycle;
    using MLambda.Actors.Core;
    using MLambda.Actors.Monitoring.Core;
    using MLambda.Actors.Network;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Fortress;
    using MLambda.Actors.Fortress.Core;
    using MLambda.Actors.Satellite;
    using MLambda.Actors.Satellite.Abstraction;
    using MLambda.Actors.Satellite.Resolver;

    /// <summary>
    /// Dependency injection registration for asteroid node services.
    /// </summary>
    public static class Register
    {
        /// <summary>
        /// Configures a complete asteroid node with all required services:
        /// actor system, network transport, lightweight message dispatching,
        /// and round-robin cluster routing. Asteroids are gateway nodes that
        /// do not host user actors.
        /// Returns a built <see cref="IActorCatalog"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>A fully configured <see cref="IActorCatalog"/>.</returns>
        public static IActorCatalog BuildAsteroidNode(
            this IServiceCollection services,
            Action<ActorCatalogConfig> configure)
        {
            var config = new ActorCatalogConfig();
            configure(config);

            var localEndpoint = config.LocalEndpoint;

            // Core actor system.
            services.AddActor();
            services.AddSingleton(config);
            services.AddSingleton(localEndpoint);
            services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
            // Fortress mTLS (register before transport so ITlsProvider is available).
            var fortressConfig = FortressConfig.FromEnvironment();
            if (fortressConfig.Enabled)
            {
                services.AddFortressClient(fortressConfig);
            }

            services.AddSingleton<ITransport>(provider =>
                new TcpTransport(
                    provider.GetRequiredService<NodeEndpoint>(),
                    provider.GetRequiredService<IEventStream>(),
                    provider.GetService<ITlsProvider>()));

            // Actor resolver (needed by AsteroidMessageDispatcher for topology routing).
            var typeRegistry = new ActorTypeRegistry(Array.Empty<Type>());
            services.AddSingleton(typeRegistry);
            services.AddSingleton<IActorResolver, ActorResolverService>();

            // Pending requests for Ask correlation.
            services.AddSingleton<ConcurrentDictionary<Guid, TaskCompletionSource<object>>>();

            // Asteroid-specific services.
            services.AddSingleton<AsteroidMessageDispatcher>();
            services.AddTransient<DispatcherActor>();
            services.AddSingleton<IAsteroidService, AsteroidService>();

            // Monitoring (if enabled).
            if (config.EnableMonitoring)
            {
                services.AddActorMonitoring(m =>
                {
                    m.NodeId = config.NodeId;
                    m.OtlpEndpoint = config.OtlpEndpoint;
                });
            }

            var provider = services.BuildServiceProvider();
            return new AsteroidCatalog(provider);
        }
    }
}
