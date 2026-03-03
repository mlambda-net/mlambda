// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StateActor.cs" company="MLambda">
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

namespace MLambda.Actors.Cluster
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Cluster.Abstraction;
    using MLambda.Actors.Gossip.Data;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Cluster-side system actor that persists messages using gossip-replicated GStacks.
    /// Each route gets its own FIFO queue (GStack) for durable message persistence.
    /// </summary>
    [Route("state")]
    public class StateActor : Actor
    {
        private readonly GossipDataReplicator replicator;
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;
        private readonly string nodeId;
        private readonly Dictionary<string, GStack<PersistMessage>> routeQueues;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateActor"/> class.
        /// </summary>
        /// <param name="replicator">The CRDT replicator for gossip sync.</param>
        /// <param name="transport">The transport layer for sending responses.</param>
        /// <param name="serializer">The message serializer.</param>
        public StateActor(
            GossipDataReplicator replicator,
            ITransport transport,
            IMessageSerializer serializer)
        {
            this.replicator = replicator;
            this.transport = transport;
            this.serializer = serializer;
            this.nodeId = "state-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            this.routeQueues = new Dictionary<string, GStack<PersistMessage>>(
                StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        protected override Behavior Receive(object data)
            => data switch
            {
                PersistMessage msg => Actor.Behavior<Unit, PersistMessage>(
                    this.HandlePersist, msg),
                ConsumeMessage msg => Actor.Behavior<Unit, ConsumeMessage>(
                    this.HandleConsume, msg),
                GetPendingMessages msg => Actor.Behavior<PendingMessagesResult, GetPendingMessages>(
                    this.HandleGetPending, msg),
                FlushMessage msg => Actor.Behavior<Unit, FlushMessage>(
                    this.HandleFlush, msg),
                _ => Actor.Ignore,
            };

        private GStack<PersistMessage> GetOrCreateQueue(string route)
        {
            if (!this.routeQueues.TryGetValue(route, out var queue))
            {
                var stateId = $"state-queue-{route}";
                queue = new GStack<PersistMessage>(stateId, this.nodeId);
                this.routeQueues[route] = queue;
                this.replicator.Register(stateId, queue);
            }

            return queue;
        }

        private IObservable<Unit> HandlePersist(PersistMessage msg)
        {
            var queue = this.GetOrCreateQueue(msg.Route);
            queue.Push(msg);
            return Actor.Done;
        }

        private IObservable<Unit> HandleConsume(ConsumeMessage msg)
        {
            if (this.routeQueues.TryGetValue(msg.Route, out var queue))
            {
                queue.Pop();
            }

            return Actor.Done;
        }

        private IObservable<PendingMessagesResult> HandleGetPending(GetPendingMessages msg)
        {
            var messages = new List<PersistMessage>();

            if (this.routeQueues.TryGetValue(msg.Route, out var queue))
            {
                messages = queue.PeekAll()
                    .Select(entry => entry.Value)
                    .ToList();
            }

            var result = new PendingMessagesResult
            {
                Route = msg.Route,
                Messages = messages,
            };

            // Actively send to DeliveryActor since topology messages are
            // fire-and-forget (Tell) and the return value would be lost.
            this.SendLocalMessage("delivery", result);

            return Observable.Return(result);
        }

        private IObservable<Unit> HandleFlush(FlushMessage msg)
        {
            if (this.routeQueues.TryGetValue(msg.Route, out var queue))
            {
                var entries = queue.PeekAll();
                var target = entries.FirstOrDefault(e => e.Value.CorrelationId == msg.CorrelationId);
                if (target.Value != null)
                {
                    queue.Pop();
                }
            }

            return Actor.Done;
        }

        private void SendLocalMessage(string targetRoute, object message)
        {
            var envelope = new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetRoute = targetRoute,
                SourceNode = this.transport.LocalEndpoint,
                Type = EnvelopeType.Topology,
                PayloadTypeName = this.serializer.GetTypeName(message),
                PayloadBytes = this.serializer.Serialize(message),
            };

            this.transport.Send(this.transport.LocalEndpoint, envelope)
                .Subscribe(_ => { }, ex => { });
        }
    }
}
