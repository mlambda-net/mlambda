// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RemoteMessageDispatcher.cs" company="MLambda">
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
    using System.Threading.Tasks;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Core;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Satellite.Abstraction;

    /// <summary>
    /// Dispatches incoming remote envelopes to local actors and correlates responses.
    /// Supports both actor-id-based and route-based addressing.
    /// </summary>
    public class RemoteMessageDispatcher : IRemoteMessageDispatcher
    {
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;
        private readonly IBucket bucket;
        private readonly IActorResolver resolver;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<object>> pendingRequests;

        private IDisposable subscription;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteMessageDispatcher"/> class.
        /// </summary>
        /// <param name="transport">The transport layer.</param>
        /// <param name="serializer">The message serializer.</param>
        /// <param name="bucket">The local actor container.</param>
        /// <param name="resolver">The actor resolver for route-based addressing.</param>
        /// <param name="pendingRequests">Shared pending requests for response correlation.</param>
        public RemoteMessageDispatcher(
            ITransport transport,
            IMessageSerializer serializer,
            IBucket bucket,
            IActorResolver resolver,
            ConcurrentDictionary<Guid, TaskCompletionSource<object>> pendingRequests)
        {
            this.transport = transport;
            this.serializer = serializer;
            this.bucket = bucket;
            this.resolver = resolver;
            this.pendingRequests = pendingRequests;
        }

        /// <inheritdoc/>
        public void Start()
        {
            this.subscription = this.transport.IncomingMessages
                .Where(e => e.Type == EnvelopeType.Tell
                    || e.Type == EnvelopeType.Ask
                    || e.Type == EnvelopeType.Response
                    || e.Type == EnvelopeType.Topology)
                .Subscribe(this.HandleEnvelope);
        }

        /// <inheritdoc/>
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
                var isRouteBased = !string.IsNullOrEmpty(envelope.TargetRoute);

                switch (envelope.Type)
                {
                    case EnvelopeType.Tell:
                        if (isRouteBased)
                        {
                            this.HandleTellByRoute(envelope);
                        }
                        else
                        {
                            this.HandleTell(envelope);
                        }

                        break;
                    case EnvelopeType.Ask:
                        if (isRouteBased)
                        {
                            this.HandleAskByRoute(envelope);
                        }
                        else
                        {
                            this.HandleAsk(envelope);
                        }

                        break;
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
                // Envelope processing errors are silently dropped.
            }
        }

        private void HandleTell(Envelope envelope)
        {
            var payload = this.serializer.Deserialize(envelope.PayloadBytes, envelope.PayloadTypeName);
            var process = this.bucket.Filter(p => p.Id == envelope.TargetActorId).FirstOrDefault();

            if (process?.Current?.MailBox != null)
            {
                var message = new Communication.Asynchronous(payload);
                process.Current.MailBox.Add(message);
            }
        }

        private void HandleTellByRoute(Envelope envelope)
        {
            var payload = this.serializer.Deserialize(envelope.PayloadBytes, envelope.PayloadTypeName);
            var address = this.resolver.Resolve(envelope.TargetRoute);

            address?.Send(payload).Subscribe(_ => { }, ex => { });
        }

        private void HandleAsk(Envelope envelope)
        {
            var payload = this.serializer.Deserialize(envelope.PayloadBytes, envelope.PayloadTypeName);
            var process = this.bucket.Filter(p => p.Id == envelope.TargetActorId).FirstOrDefault();

            if (process?.Current?.MailBox != null)
            {
                var message = new Communication.Synchronous(payload);
                process.Current.MailBox.Add(message);
                message.ToObservable<object>().Subscribe(
                    response => this.SendResponse(envelope, response),
                    ex => { });
            }
        }

        private void HandleAskByRoute(Envelope envelope)
        {
            var payload = this.serializer.Deserialize(envelope.PayloadBytes, envelope.PayloadTypeName);
            var address = this.resolver.Resolve(envelope.TargetRoute);

            if (address != null)
            {
                address.Send<object, object>(payload).Subscribe(
                    response => this.SendResponse(envelope, response),
                    ex => { });
            }
        }

        private void SendResponse(Envelope originalEnvelope, object response)
        {
            var responseEnvelope = new Envelope
            {
                CorrelationId = originalEnvelope.CorrelationId,
                TargetActorId = originalEnvelope.SourceActorId,
                SourceActorId = originalEnvelope.TargetActorId,
                SourceNode = this.transport.LocalEndpoint,
                Type = EnvelopeType.Response,
                PayloadTypeName = this.serializer.GetTypeName(response),
                PayloadBytes = this.serializer.Serialize(response),
            };

            this.transport.Send(originalEnvelope.SourceNode, responseEnvelope)
                .Subscribe(_ => { }, ex => { });
        }

        private void HandleTopology(Envelope envelope)
        {
            if (string.IsNullOrEmpty(envelope.TargetRoute))
            {
                return;
            }

            var payload = this.serializer.Deserialize(envelope.PayloadBytes, envelope.PayloadTypeName);
            var address = this.resolver.Resolve(envelope.TargetRoute);

            address?.Send(payload).Subscribe(_ => { }, ex => { });
        }

        private void HandleResponse(Envelope envelope)
        {
            if (this.pendingRequests.TryRemove(envelope.CorrelationId, out var tcs))
            {
                var payload = this.serializer.Deserialize(envelope.PayloadBytes, envelope.PayloadTypeName);
                tcs.TrySetResult(payload);
            }
        }
    }
}
