// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IActorResolver.cs" company="MLambda">
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
    using MLambda.Actors.Abstraction;

    /// <summary>
    /// Resolves route names to actor addresses, creating actors lazily on demand.
    /// </summary>
    public interface IActorResolver
    {
        /// <summary>
        /// Returns the local actor address for the given route, or null if not local.
        /// Does not create actors.
        /// </summary>
        /// <param name="route">The route name.</param>
        /// <returns>The local address, or null.</returns>
        IAddress ResolveLocal(string route);

        /// <summary>
        /// Resolves the route to a local actor address, creating the actor if necessary.
        /// Returns null if the actor type is not registered on this node.
        /// </summary>
        /// <param name="route">The route name.</param>
        /// <returns>The local address, or null.</returns>
        IAddress Resolve(string route);

        /// <summary>
        /// Invalidates the cached address for a route (used during failover).
        /// </summary>
        /// <param name="route">The route name.</param>
        void Invalidate(string route);

        /// <summary>
        /// Registers an externally-created actor address for a route.
        /// Used by WorkerActor to register children it has spawned.
        /// </summary>
        /// <param name="route">The route name.</param>
        /// <param name="address">The actor address.</param>
        void Register(string route, IAddress address);
    }
}
