// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RouteAddress.cs" company="MLambda">
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
    /// An <see cref="IAddress"/> implementation that routes messages by route name.
    /// Handles local delivery and remote forwarding via cluster RouteActor.
    /// Supports parameterized routes (e.g. "manager/{id}") where each unique
    /// parameter combination creates a separate actor instance.
    /// </summary>
    public class RouteAddress : IAddress
    {
        private readonly string route;
        private readonly IActorResolver resolver;
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;
        private readonly NodeEndpoint localEndpoint;
        private readonly NodeEndpoint clusterTarget;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<object>> pendingRequests;

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteAddress"/> class.
        /// </summary>
        /// <param name="route">The route name (template or resolved).</param>
        /// <param name="resolver">The actor resolver.</param>
        /// <param name="transport">The transport layer.</param>
        /// <param name="serializer">The message serializer.</param>
        /// <param name="localEndpoint">The local node endpoint.</param>
        /// <param name="clusterTarget">The cluster node to route through.</param>
        /// <param name="pendingRequests">Shared pending requests for response correlation.</param>
        public RouteAddress(
            string route,
            IActorResolver resolver,
            ITransport transport,
            IMessageSerializer serializer,
            NodeEndpoint localEndpoint,
            NodeEndpoint clusterTarget,
            ConcurrentDictionary<Guid, TaskCompletionSource<object>> pendingRequests)
        {
            this.route = route;
            this.resolver = resolver;
            this.transport = transport;
            this.serializer = serializer;
            this.localEndpoint = localEndpoint;
            this.clusterTarget = clusterTarget;
            this.pendingRequests = pendingRequests;
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Empty;

        /// <inheritdoc/>
        public IObservable<TO> Send<TI, TO>(TI message)
        {
            var localAddress = this.resolver.ResolveLocal(this.route);
            if (localAddress != null)
            {
                return localAddress.Send<TI, TO>(message);
            }

            return this.SendAskViaCluster<TI, TO>(message, this.route, null);
        }

        /// <inheritdoc/>
        public IObservable<Unit> Send<T>(T message)
        {
            var localAddress = this.resolver.ResolveLocal(this.route);
            if (localAddress != null)
            {
                return localAddress.Send(message);
            }

            return this.SendTellViaCluster(message, this.route, null);
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
            var localAddress = this.resolver.ResolveLocal(resolvedRoute);
            if (localAddress != null)
            {
                return localAddress.Send<TI, TO>(message);
            }

            return this.SendAskViaCluster<TI, TO>(message, resolvedRoute, parameters?.ToDictionary());
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
            var localAddress = this.resolver.ResolveLocal(resolvedRoute);
            if (localAddress != null)
            {
                return localAddress.Send(message);
            }

            return this.SendTellViaCluster(message, resolvedRoute, parameters?.ToDictionary());
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

        private IObservable<TO> SendAskViaCluster<TI, TO>(
            TI message,
            string targetRoute,
            Dictionary<string, object> parameters)
        {
            return Observable.FromAsync(async () =>
            {
                var correlationId = Guid.NewGuid();
                var tcs = new TaskCompletionSource<object>();
                this.pendingRequests[correlationId] = tcs;

                try
                {
                    await this.SendDispatchWork(message, correlationId, true, targetRoute, parameters);
                    var result = await tcs.Task;
                    return (TO)result;
                }
                finally
                {
                    this.pendingRequests.TryRemove(correlationId, out _);
                }
            });
        }

        private IObservable<Unit> SendTellViaCluster<T>(
            T message,
            string targetRoute,
            Dictionary<string, object> parameters)
        {
            var correlationId = Guid.NewGuid();
            return this.SendDispatchWork(message, correlationId, false, targetRoute, parameters)
                .Select(_ => Unit.Default);
        }

        private IObservable<Unit> SendDispatchWork<T>(
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

            var envelope = new Envelope
            {
                CorrelationId = correlationId,
                TargetRoute = "route",
                SourceNode = this.localEndpoint,
                Type = EnvelopeType.Topology,
                PayloadTypeName = this.serializer.GetTypeName(dispatchWork),
                PayloadBytes = this.serializer.Serialize(dispatchWork),
            };

            return this.transport.Send(this.clusterTarget, envelope);
        }
    }
}
