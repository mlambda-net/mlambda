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

    /// <summary>
    /// Cluster-side system actor that persists messages using gossip-replicated GStacks.
    /// Each route gets its own FIFO queue (GStack) for durable message persistence.
    /// </summary>
    [Route("state")]
    public class StateActor : Actor
    {
        private readonly GossipDataReplicator replicator;
        private readonly string nodeId;
        private readonly Dictionary<string, GStack<PersistMessage>> routeQueues;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateActor"/> class.
        /// </summary>
        /// <param name="replicator">The CRDT replicator for gossip sync.</param>
        public StateActor(GossipDataReplicator replicator)
        {
            this.replicator = replicator;
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

            return Observable.Return(new PendingMessagesResult
            {
                Route = msg.Route,
                Messages = messages,
            });
        }
    }
}
