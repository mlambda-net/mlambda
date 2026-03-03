// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActorRegistry.cs" company="MLambda">
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

namespace MLambda.Actors.Satellite
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Satellite.Abstraction;

    /// <summary>
    /// Thread-safe registry mapping actor identifiers to hosting nodes.
    /// </summary>
    public class ActorRegistry : IActorRegistry
    {
        private readonly ConcurrentDictionary<Guid, NodeEndpoint> entries;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorRegistry"/> class.
        /// </summary>
        public ActorRegistry()
        {
            this.entries = new ConcurrentDictionary<Guid, NodeEndpoint>();
        }

        /// <inheritdoc/>
        public void Register(Guid actorId, NodeEndpoint node)
        {
            this.entries[actorId] = node;
        }

        /// <inheritdoc/>
        public NodeEndpoint Lookup(Guid actorId)
        {
            this.entries.TryGetValue(actorId, out var node);
            return node;
        }

        /// <inheritdoc/>
        public void Remove(Guid actorId)
        {
            this.entries.TryRemove(actorId, out _);
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<Guid, NodeEndpoint> GetAll()
        {
            return new Dictionary<Guid, NodeEndpoint>(this.entries);
        }
    }
}
