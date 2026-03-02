// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEventStream.cs" company="MLambda">
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

namespace MLambda.Actors.Abstraction.Core
{
    using System;

    /// <summary>
    /// The event stream interface for publishing and subscribing to events.
    /// </summary>
    public interface IEventStream
    {
        /// <summary>
        /// Publishes a message to the event stream.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message to publish.</param>
        void Publish<T>(T message);

        /// <summary>
        /// Subscribes to messages of a specific type.
        /// </summary>
        /// <typeparam name="T">The type of the message to subscribe to.</typeparam>
        /// <param name="handler">The handler to invoke when a message is received.</param>
        /// <returns>A disposable subscription.</returns>
        IDisposable Subscribe<T>(Action<T> handler);
    }
}
