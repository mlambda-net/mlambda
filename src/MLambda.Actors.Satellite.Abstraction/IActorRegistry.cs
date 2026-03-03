// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IActorRegistry.cs" company="MLambda">
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

namespace MLambda.Actors.Satellite.Abstraction
{
    using System;
    using System.Collections.Generic;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Registry that maps actor identifiers to their hosting nodes.
    /// </summary>
    public interface IActorRegistry
    {
        /// <summary>
        /// Registers an actor on a specific node.
        /// </summary>
        /// <param name="actorId">The actor identifier.</param>
        /// <param name="node">The hosting node endpoint.</param>
        void Register(Guid actorId, NodeEndpoint node);

        /// <summary>
        /// Looks up which node hosts the given actor.
        /// </summary>
        /// <param name="actorId">The actor identifier.</param>
        /// <returns>The node endpoint, or null if not found.</returns>
        NodeEndpoint Lookup(Guid actorId);

        /// <summary>
        /// Removes an actor from the registry.
        /// </summary>
        /// <param name="actorId">The actor identifier.</param>
        void Remove(Guid actorId);

        /// <summary>
        /// Gets all registered actor-to-node mappings.
        /// </summary>
        /// <returns>The collection of mappings.</returns>
        IReadOnlyDictionary<Guid, NodeEndpoint> GetAll();
    }
}
