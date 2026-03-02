// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventStream.cs" company="MLambda">
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

namespace MLambda.Actors
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using MLambda.Actors.Abstraction.Core;

    /// <summary>
    /// The event stream pub/sub bus for system-wide events.
    /// </summary>
    public class EventStream : IEventStream
    {
        private readonly ConcurrentDictionary<Type, List<Delegate>> subscribers;

        private readonly object locker;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStream"/> class.
        /// </summary>
        public EventStream()
        {
            this.subscribers = new ConcurrentDictionary<Type, List<Delegate>>();
            this.locker = new object();
        }

        /// <summary>
        /// Publishes a message to all subscribers of type T.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <typeparam name="T">The type of the message.</typeparam>
        public void Publish<T>(T message)
        {
            if (this.subscribers.TryGetValue(typeof(T), out var handlers))
            {
                List<Delegate> snapshot;
                lock (this.locker)
                {
                    snapshot = new List<Delegate>(handlers);
                }

                foreach (var handler in snapshot)
                {
                    ((Action<T>)handler)(message);
                }
            }
        }

        /// <summary>
        /// Subscribes a handler for messages of type T.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <returns>A disposable subscription.</returns>
        public IDisposable Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            var list = this.subscribers.GetOrAdd(type, _ => new List<Delegate>());

            lock (this.locker)
            {
                list.Add(handler);
            }

            return new Subscription(() =>
            {
                lock (this.locker)
                {
                    list.Remove(handler);
                }
            });
        }

        /// <summary>
        /// The subscription disposable wrapper.
        /// </summary>
        private class Subscription : IDisposable
        {
            private readonly Action unsubscribe;

            /// <summary>
            /// Initializes a new instance of the <see cref="Subscription"/> class.
            /// </summary>
            /// <param name="unsubscribe">The unsubscribe action.</param>
            public Subscription(Action unsubscribe)
            {
                this.unsubscribe = unsubscribe;
            }

            /// <summary>
            /// Disposes the subscription.
            /// </summary>
            public void Dispose()
            {
                this.unsubscribe();
            }
        }
    }
}
