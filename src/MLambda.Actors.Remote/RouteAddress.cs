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

namespace MLambda.Actors.Remote
{
    using System;
    using System.Collections.Concurrent;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Cluster.Abstraction;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Remote.Abstraction;

    /// <summary>
    /// An <see cref="IAddress"/> implementation that routes messages by route name.
    /// Handles local delivery and remote forwarding via cluster RouteActor.
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
        /// <param name="route">The route name.</param>
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

            return this.SendAskViaCluster<TI, TO>(message);
        }

        /// <inheritdoc/>
        public IObservable<Unit> Send<T>(T message)
        {
            var localAddress = this.resolver.ResolveLocal(this.route);
            if (localAddress != null)
            {
                return localAddress.Send(message);
            }

            return this.SendTellViaCluster(message);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        private IObservable<TO> SendAskViaCluster<TI, TO>(TI message)
        {
            return Observable.FromAsync(async () =>
            {
                var correlationId = Guid.NewGuid();
                var tcs = new TaskCompletionSource<object>();
                this.pendingRequests[correlationId] = tcs;

                try
                {
                    await this.SendDispatchWork(message, correlationId, true);
                    var result = await tcs.Task;
                    return (TO)result;
                }
                finally
                {
                    this.pendingRequests.TryRemove(correlationId, out _);
                }
            });
        }

        private IObservable<Unit> SendTellViaCluster<T>(T message)
        {
            var correlationId = Guid.NewGuid();
            return this.SendDispatchWork(message, correlationId, false)
                .Select(_ => Unit.Default);
        }

        private IObservable<Unit> SendDispatchWork<T>(T message, Guid correlationId, bool isAsk)
        {
            var dispatchWork = new DispatchWork
            {
                CorrelationId = correlationId,
                TargetRoute = this.route,
                PayloadTypeName = this.serializer.GetTypeName(message),
                PayloadBytes = this.serializer.Serialize(message),
                OriginNode = this.localEndpoint,
                IsAsk = isAsk,
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
