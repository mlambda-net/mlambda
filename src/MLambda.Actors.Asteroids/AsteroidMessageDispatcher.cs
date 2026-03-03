// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AsteroidMessageDispatcher.cs" company="MLambda">
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
    using System.Threading.Tasks;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Cluster.Abstraction;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Satellite.Abstraction;

    /// <summary>
    /// Lightweight message dispatcher for asteroid nodes.
    /// Only handles Response envelopes (for Ask correlation)
    /// and Topology envelopes (for cluster topology updates).
    /// </summary>
    public class AsteroidMessageDispatcher : IDisposable
    {
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;
        private readonly IActorResolver resolver;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<object>> pendingRequests;

        private IDisposable subscription;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsteroidMessageDispatcher"/> class.
        /// </summary>
        /// <param name="transport">The transport layer.</param>
        /// <param name="serializer">The message serializer.</param>
        /// <param name="resolver">The actor resolver for route-based addressing.</param>
        /// <param name="pendingRequests">Shared pending requests for response correlation.</param>
        public AsteroidMessageDispatcher(
            ITransport transport,
            IMessageSerializer serializer,
            IActorResolver resolver,
            ConcurrentDictionary<Guid, TaskCompletionSource<object>> pendingRequests)
        {
            this.transport = transport;
            this.serializer = serializer;
            this.resolver = resolver;
            this.pendingRequests = pendingRequests;
        }

        /// <summary>
        /// Starts listening for incoming Response and Topology envelopes.
        /// </summary>
        public void Start()
        {
            this.subscription = this.transport.IncomingMessages
                .Where(e => e.Type == EnvelopeType.Response
                    || e.Type == EnvelopeType.Topology)
                .Subscribe(this.HandleEnvelope);
        }

        /// <summary>
        /// Stops listening for incoming envelopes.
        /// </summary>
        public void Stop()
        {
            this.subscription?.Dispose();
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

        private void HandleEnvelope(Envelope envelope)
        {
            try
            {
                switch (envelope.Type)
                {
                    case EnvelopeType.Response:
                        this.HandleResponse(envelope);
                        break;
                    case EnvelopeType.Topology:
                        this.HandleTopology(envelope);
                        break;
                }
            }
            catch (Exception)
            {
            }
        }

        private void HandleResponse(Envelope envelope)
        {
            if (this.pendingRequests.TryRemove(envelope.CorrelationId, out var tcs))
            {
                var payload = this.serializer.Deserialize(envelope.PayloadBytes, envelope.PayloadTypeName);
                tcs.TrySetResult(payload);
            }
        }

        private void HandleTopology(Envelope envelope)
        {
            if (string.IsNullOrEmpty(envelope.TargetRoute))
            {
                return;
            }

            var payload = this.serializer.Deserialize(envelope.PayloadBytes, envelope.PayloadTypeName);
            var address = this.resolver.Resolve(envelope.TargetRoute);

            if (address != null)
            {
                address.Send(payload).Subscribe(_ => { }, ex => { });
            }
        }
    }
}
