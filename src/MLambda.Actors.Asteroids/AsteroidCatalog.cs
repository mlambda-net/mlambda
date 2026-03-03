// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AsteroidCatalog.cs" company="MLambda">
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
    using System.Collections.Concurrent;
    using System.Reactive.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Abstraction.Routing;
    using MLambda.Actors.Asteroids.Core;
    using MLambda.Actors.Asteroids.Lifecycle;
    using MLambda.Actors.Fortress;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Satellite.Abstraction;

    /// <summary>
    /// A lightweight asteroid node that connects to the cluster as a gateway.
    /// Routes messages through a local <see cref="DispatcherActor"/> using
    /// round-robin cluster node selection. Does not host user actors.
    /// </summary>
    public class AsteroidCatalog : IActorCatalog
    {
        private readonly ServiceProvider provider;
        private readonly ActorCatalogConfig config;

        private ITransport transport;
        private AsteroidMessageDispatcher dispatcher;
        private IAsteroidService asteroidService;
        private FortressClientService fortressClientService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsteroidCatalog"/> class.
        /// </summary>
        /// <param name="provider">The built service provider.</param>
        public AsteroidCatalog(ServiceProvider provider)
        {
            this.provider = provider;
            this.config = provider.GetRequiredService<ActorCatalogConfig>();
        }

        /// <summary>
        /// Builds a fully configured asteroid node from the given configuration.
        /// </summary>
        /// <param name="configure">The configuration action.</param>
        /// <returns>A fully configured <see cref="IActorCatalog"/>.</returns>
        public static IActorCatalog Build(Action<ActorCatalogConfig> configure)
        {
            var services = new ServiceCollection();
            return services.BuildAsteroidNode(configure);
        }

        /// <inheritdoc/>
        public IServiceProvider Services => this.provider;

        /// <inheritdoc/>
        public IUserContext User => this.provider.GetRequiredService<IUserContext>();

        /// <inheritdoc/>
        public async Task Start()
        {
            // Transport.
            this.transport = this.provider.GetRequiredService<ITransport>();
            await this.transport.Start();

            // Asteroid message dispatcher (Response + Topology only).
            this.dispatcher = this.provider.GetRequiredService<AsteroidMessageDispatcher>();
            this.dispatcher.Start();

            // Spawn the local DispatcherActor and register it in the resolver
            // so AsteroidRouteAddress can find it via resolver.Resolve("dispatcher").
            var systemContext = this.provider.GetRequiredService<ISystemContext>();
            var dispatcherAddr = await systemContext.Spawn<DispatcherActor>();
            var resolver = this.provider.GetRequiredService<IActorResolver>();
            resolver.Register("dispatcher", dispatcherAddr);

            // Fortress security (request cert from cluster).
            this.fortressClientService = this.provider.GetService<FortressClientService>();
            this.fortressClientService?.Start();

            // Asteroid lifecycle service (register + heartbeat).
            this.asteroidService = this.provider.GetRequiredService<IAsteroidService>();
            this.asteroidService.Start();
        }

        /// <inheritdoc/>
        public async Task Stop()
        {
            this.fortressClientService?.Stop();
            this.asteroidService?.Stop();
            this.dispatcher?.Stop();

            if (this.transport != null)
            {
                await this.transport.Stop();
            }
        }

        /// <inheritdoc/>
        public Task<IAddress> Spawn<T>()
            where T : IActor
        {
            throw new NotSupportedException(
                "Asteroid nodes do not host actors. Use For<T>() to route messages through the cluster.");
        }

        /// <inheritdoc/>
        public IAddress For<T>()
            where T : IActor
        {
            var route = typeof(T).GetCustomAttribute<RouteAttribute>()?.Name
                ?? throw new InvalidOperationException(
                    $"Actor type {typeof(T).Name} does not have a [Route] attribute.");

            return new AsteroidRouteAddress(
                route,
                this.provider.GetRequiredService<IActorResolver>(),
                this.provider.GetRequiredService<IMessageSerializer>(),
                this.provider.GetRequiredService<NodeEndpoint>(),
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

            return new AsteroidRouteAddress(
                resolvedRoute,
                this.provider.GetRequiredService<IActorResolver>(),
                this.provider.GetRequiredService<IMessageSerializer>(),
                this.provider.GetRequiredService<NodeEndpoint>(),
                this.provider.GetRequiredService<ConcurrentDictionary<Guid, TaskCompletionSource<object>>>());
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Stop().GetAwaiter().GetResult();
            this.provider?.Dispose();
        }
    }
}
