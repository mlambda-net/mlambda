// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MailBox.cs" company="MLambda">
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Core;

    /// <summary>
    /// The mail box class.
    /// </summary>
    public class MailBox : IMailBox
    {
        private readonly ICollector collector;

        private readonly Queue<IMessage> messages;

        private readonly object locker;

        private LifeCycle state;

        private bool suspended;

        /// <summary>
        /// Initializes a new instance of the <see cref="MailBox"/> class.
        /// </summary>
        /// <param name="collector">the collector.</param>
        public MailBox(ICollector collector)
        {
            this.state = LifeCycle.Running;
            this.collector = collector;
            this.Id = Guid.NewGuid();
            this.locker = new object();
            this.messages = new Queue<IMessage>();
        }

        /// <summary>
        /// The mailbox life cycle.
        /// </summary>
        public enum LifeCycle
        {
            /// <summary>
            /// The mailbox is running.
            /// </summary>
            Running = 0,

            /// <summary>
            /// The mailbox is disposed.
            /// </summary>
            Disposed = 1,
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets a value indicating whether the mailbox is stopped.
        /// </summary>
        public bool IsStopped
        {
            get
            {
                lock (this.locker)
                {
                    return this.state == LifeCycle.Disposed;
                }
            }
        }

        /// <summary>
        /// Add the message to the mailbox.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Add(IMessage message)
        {
            lock (this.locker)
            {
                if (this.state == LifeCycle.Disposed)
                {
                    throw new ObjectDisposedException(nameof(MailBox));
                }

                this.messages.Enqueue(message);
                Monitor.Pulse(this.locker);
            }
        }

        /// <summary>
        /// Takes the message in the queue.
        /// </summary>
        /// <returns>The feature.</returns>
        public IMessage Take()
        {
            lock (this.locker)
            {
                while (this.state == LifeCycle.Running && (!this.messages.Any() || this.suspended))
                {
                    Monitor.Wait(this.locker);
                }

                if (this.state == LifeCycle.Disposed)
                {
                    return null;
                }

                return this.messages.Dequeue();
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
            lock (this.locker)
            {
                if (this.state == LifeCycle.Disposed)
                {
                    return;
                }

                this.state = LifeCycle.Disposed;
                this.messages.Clear();
                Monitor.PulseAll(this.locker);
            }

            this.collector.Collect(this.Id);
        }

        /// <summary>
        /// Suspends message delivery.
        /// </summary>
        public void Suspend()
        {
            lock (this.locker)
            {
                this.suspended = true;
            }
        }

        /// <summary>
        /// Resumes message delivery.
        /// </summary>
        public void Resume()
        {
            lock (this.locker)
            {
                this.suspended = false;
                Monitor.PulseAll(this.locker);
            }
        }

        /// <summary>
        /// Cleans the message mailbox.
        /// </summary>
        public void Clean()
        {
            lock (this.locker)
            {
                this.messages.Clear();
            }
        }
    }
}
