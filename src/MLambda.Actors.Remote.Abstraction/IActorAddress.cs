// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IActorAddress.cs" company="MLambda">
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

namespace MLambda.Actors.Remote.Abstraction
{
    using System;
    using System.Threading.Tasks;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Context;

    /// <summary>
    /// Represents a remote actor node with its full lifecycle.
    /// </summary>
    public interface IActorAddress : IDisposable
    {
        /// <summary>
        /// Gets the service provider for resolving services.
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// Gets the user context for spawning actors.
        /// </summary>
        IUserContext User { get; }

        /// <summary>
        /// Starts the node: transport, cluster, dispatcher, and broker.
        /// </summary>
        /// <returns>A task representing the async start operation.</returns>
        Task Start();

        /// <summary>
        /// Stops the node gracefully.
        /// </summary>
        /// <returns>A task representing the async stop operation.</returns>
        Task Stop();

        /// <summary>
        /// Spawns an actor on this node.
        /// </summary>
        /// <typeparam name="T">The actor type.</typeparam>
        /// <returns>The actor address.</returns>
        Task<IAddress> Spawn<T>()
            where T : IActor;

        /// <summary>
        /// Returns a route-based address for the actor type, using its
        /// <see cref="MLambda.Actors.Abstraction.Annotation.RouteAttribute"/>
        /// to determine the route name. Messages sent to this address are
        /// routed to the actor owning the route, creating the actor lazily
        /// if necessary.
        /// </summary>
        /// <typeparam name="T">The actor type decorated with a Route attribute.</typeparam>
        /// <returns>A route-based actor address.</returns>
        IAddress For<T>()
            where T : IActor;
    }
}
