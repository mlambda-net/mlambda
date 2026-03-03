// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IAddress.cs" company="MLambda">
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
    using System;
    using System.Reactive;

    /// <summary>
    /// The actor proxy.
    /// </summary>
    public interface IAddress : IDisposable
    {
        /// <summary>
        /// Gets the unique identifier of the address.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Tells the message to the actor.
        /// </summary>
        /// <param name="message">the message.</param>
        /// <typeparam name="TI">the type in.</typeparam>
        /// <typeparam name="TO">the type out.</typeparam>
        /// <returns>The response of the the actor.</returns>
        IObservable<TO> Send<TI, TO>(TI message);

        /// <summary>
        /// Tells the message to the actor.
        /// </summary>
        /// <param name="message">the message.</param>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <returns>The response of the the actor.</returns>
        IObservable<Unit> Send<T>(T message);

        /// <summary>
        /// Sends a request message to the actor with route parameters.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="parameters">The route parameters for parameterized routes.</param>
        /// <typeparam name="TI">The input type.</typeparam>
        /// <typeparam name="TO">The output type.</typeparam>
        /// <returns>The response of the actor.</returns>
        IObservable<TO> Send<TI, TO>(TI message, Parameter parameters) => this.Send<TI, TO>(message);

        /// <summary>
        /// Tells a message to the actor with route parameters.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="parameters">The route parameters for parameterized routes.</param>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <returns>The response of the actor.</returns>
        IObservable<Unit> Send<T>(T message, Parameter parameters) => this.Send<T>(message);
    }
}