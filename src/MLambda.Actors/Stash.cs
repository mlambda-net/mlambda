// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Stash.cs" company="MLambda">
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
    using System.Collections.Generic;
    using MLambda.Actors.Abstraction;

    /// <summary>
    /// Concrete implementation of IStash that temporarily stores messages
    /// and re-enqueues them to the mailbox when unstashed.
    /// </summary>
    public class MessageStash : IStash
    {
        private readonly Stack<IMessage> stashed;

        private readonly IMailBox mailBox;

        private IMessage current;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageStash"/> class.
        /// </summary>
        /// <param name="mailBox">The mailbox to re-enqueue messages to.</param>
        public MessageStash(IMailBox mailBox)
        {
            this.mailBox = mailBox;
            this.stashed = new Stack<IMessage>();
        }

        /// <summary>
        /// Gets the number of stashed messages.
        /// </summary>
        public int Count => this.stashed.Count;

        /// <summary>
        /// Sets the current message being processed for stashing.
        /// </summary>
        /// <param name="message">The current message.</param>
        internal void SetCurrent(IMessage message)
        {
            this.current = message;
        }

        /// <summary>
        /// Stashes the current message for later processing.
        /// </summary>
        void IStash.Stash()
        {
            if (this.current != null)
            {
                this.stashed.Push(this.current);
                this.current = null;
            }
        }

        /// <summary>
        /// Unstashes the most recently stashed message by re-enqueuing it to the mailbox.
        /// </summary>
        public void Unstash()
        {
            if (this.stashed.Count > 0)
            {
                var message = this.stashed.Pop();
                this.mailBox.Add(message);
            }
        }

        /// <summary>
        /// Unstashes all stashed messages by re-enqueuing them to the mailbox
        /// in the order they were originally received.
        /// </summary>
        public void UnstashAll()
        {
            var messages = new List<IMessage>(this.stashed);
            messages.Reverse();
            this.stashed.Clear();

            foreach (var message in messages)
            {
                this.mailBox.Add(message);
            }
        }
    }
}
