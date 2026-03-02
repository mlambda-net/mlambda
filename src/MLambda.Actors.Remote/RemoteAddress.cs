// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RemoteAddress.cs" company="MLambda">
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
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// An IAddress implementation that sends messages to a remote actor via transport.
    /// </summary>
    public class RemoteAddress : IAddress
    {
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;
        private readonly NodeEndpoint targetNode;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<object>> pendingRequests;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteAddress"/> class.
        /// </summary>
        /// <param name="actorId">The remote actor identifier.</param>
        /// <param name="targetNode">The node hosting the remote actor.</param>
        /// <param name="localNode">The local node endpoint.</param>
        /// <param name="transport">The transport layer.</param>
        /// <param name="serializer">The message serializer.</param>
        /// <param name="pendingRequests">Shared pending requests for response correlation.</param>
        public RemoteAddress(
            Guid actorId,
            NodeEndpoint targetNode,
            NodeEndpoint localNode,
            ITransport transport,
            IMessageSerializer serializer,
            ConcurrentDictionary<Guid, TaskCompletionSource<object>> pendingRequests)
        {
            this.Id = actorId;
            this.targetNode = targetNode;
            this.LocalNode = localNode;
            this.transport = transport;
            this.serializer = serializer;
            this.pendingRequests = pendingRequests;
        }

        /// <inheritdoc/>
        public Guid Id { get; }

        /// <summary>
        /// Gets the local node endpoint.
        /// </summary>
        public NodeEndpoint LocalNode { get; }

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
                    var envelope = new Envelope
                    {
                        CorrelationId = correlationId,
                        TargetActorId = this.Id,
                        SourceActorId = Guid.Empty,
                        SourceNode = this.LocalNode,
                        Type = EnvelopeType.Ask,
                        PayloadTypeName = this.serializer.GetTypeName(message),
                        PayloadBytes = this.serializer.Serialize(message),
                    };

                    await this.transport.Send(this.targetNode, envelope);
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
            var envelope = new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetActorId = this.Id,
                SourceActorId = Guid.Empty,
                SourceNode = this.LocalNode,
                Type = EnvelopeType.Tell,
                PayloadTypeName = this.serializer.GetTypeName(message),
                PayloadBytes = this.serializer.Serialize(message),
            };

            return this.transport.Send(this.targetNode, envelope)
                .Select(_ => Unit.Default);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
