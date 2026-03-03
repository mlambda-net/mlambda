// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AsteroidRouteAddress.cs" company="MLambda">
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
    using System.Collections.Generic;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Routing;
    using MLambda.Actors.Cluster.Abstraction;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Satellite.Abstraction;

    /// <summary>
    /// An <see cref="IAddress"/> implementation for asteroid nodes that routes
    /// messages through the local <see cref="DispatcherActor"/> which applies
    /// round-robin cluster node selection. Supports parameterized routes.
    /// </summary>
    public class AsteroidRouteAddress : IAddress
    {
        private readonly string route;
        private readonly IActorResolver resolver;
        private readonly IMessageSerializer serializer;
        private readonly NodeEndpoint localEndpoint;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<object>> pendingRequests;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsteroidRouteAddress"/> class.
        /// </summary>
        /// <param name="route">The route name (template or resolved).</param>
        /// <param name="resolver">The actor resolver.</param>
        /// <param name="serializer">The message serializer.</param>
        /// <param name="localEndpoint">The local node endpoint.</param>
        /// <param name="pendingRequests">Shared pending requests for response correlation.</param>
        public AsteroidRouteAddress(
            string route,
            IActorResolver resolver,
            IMessageSerializer serializer,
            NodeEndpoint localEndpoint,
            ConcurrentDictionary<Guid, TaskCompletionSource<object>> pendingRequests)
        {
            this.route = route;
            this.resolver = resolver;
            this.serializer = serializer;
            this.localEndpoint = localEndpoint;
            this.pendingRequests = pendingRequests;
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Empty;

        /// <inheritdoc/>
        public IObservable<TO> Send<TI, TO>(TI message)
        {
            return Observable.FromAsync(async () =>
            {
                var correlationId = Guid.NewGuid();
                var tcs = new TaskCompletionSource<object>();
                this.pendingRequests[correlationId] = tcs;

                try
                {
                    this.SendToDispatcher(message, correlationId, true, this.route, null);
                    var result = await tcs.Task;
                    return (TO)result;
                }
                finally
                {
                    this.pendingRequests.TryRemove(correlationId, out _);
                }
            });
        }

        /// <inheritdoc/>
        public IObservable<Unit> Send<T>(T message)
        {
            var correlationId = Guid.NewGuid();
            this.SendToDispatcher(message, correlationId, false, this.route, null);
            return Observable.Return(Unit.Default);
        }

        /// <summary>
        /// Sends a request message to the actor with route parameters.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="parameters">The route parameters for parameterized routes.</param>
        /// <typeparam name="TI">The input type.</typeparam>
        /// <typeparam name="TO">The output type.</typeparam>
        /// <returns>The response of the actor.</returns>
        public IObservable<TO> Send<TI, TO>(TI message, Parameter parameters)
        {
            var resolvedRoute = this.ResolveRoute(parameters);
            return Observable.FromAsync(async () =>
            {
                var correlationId = Guid.NewGuid();
                var tcs = new TaskCompletionSource<object>();
                this.pendingRequests[correlationId] = tcs;

                try
                {
                    this.SendToDispatcher(message, correlationId, true, resolvedRoute, parameters?.ToDictionary());
                    var result = await tcs.Task;
                    return (TO)result;
                }
                finally
                {
                    this.pendingRequests.TryRemove(correlationId, out _);
                }
            });
        }

        /// <summary>
        /// Tells a message to the actor with route parameters.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="parameters">The route parameters for parameterized routes.</param>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <returns>The response of the actor.</returns>
        public IObservable<Unit> Send<T>(T message, Parameter parameters)
        {
            var resolvedRoute = this.ResolveRoute(parameters);
            var correlationId = Guid.NewGuid();
            this.SendToDispatcher(message, correlationId, false, resolvedRoute, parameters?.ToDictionary());
            return Observable.Return(Unit.Default);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        private string ResolveRoute(Parameter parameters)
        {
            if (parameters == null || parameters.IsEmpty)
            {
                return this.route;
            }

            var template = new RouteTemplate(this.route);
            return template.Resolve(parameters);
        }

        private void SendToDispatcher<T>(
            T message,
            Guid correlationId,
            bool isAsk,
            string targetRoute,
            Dictionary<string, object> parameters)
        {
            var dispatchWork = new DispatchWork
            {
                CorrelationId = correlationId,
                TargetRoute = targetRoute,
                PayloadTypeName = this.serializer.GetTypeName(message),
                PayloadBytes = this.serializer.Serialize(message),
                OriginNode = this.localEndpoint,
                IsAsk = isAsk,
                Parameters = parameters,
            };

            var dispatcherAddress = this.resolver.Resolve("dispatcher");
            dispatcherAddress?.Send(dispatchWork).Subscribe(_ => { }, ex => { });
        }
    }
}
