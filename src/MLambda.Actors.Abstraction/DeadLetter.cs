// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeadLetter.cs" company="MLambda">
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

namespace MLambda.Actors.Abstraction
{
    /// <summary>
    /// Represents a message that could not be delivered to its intended recipient.
    /// </summary>
    public class DeadLetter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeadLetter"/> class.
        /// </summary>
        /// <param name="message">The undeliverable message.</param>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="recipient">The intended recipient of the message.</param>
        public DeadLetter(object message, IAddress sender, IAddress recipient)
        {
            this.Message = message;
            this.Sender = sender;
            this.Recipient = recipient;
        }

        /// <summary>
        /// Gets the undeliverable message.
        /// </summary>
        public object Message { get; }

        /// <summary>
        /// Gets the sender of the message.
        /// </summary>
        public IAddress Sender { get; }

        /// <summary>
        /// Gets the intended recipient of the message.
        /// </summary>
        public IAddress Recipient { get; }
    }
}
