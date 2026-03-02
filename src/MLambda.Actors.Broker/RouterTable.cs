// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RouterTable.cs" company="MLambda">
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

namespace MLambda.Actors.Broker
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using MLambda.Actors.Broker.Abstraction;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Thread-safe routing table mapping actor routes to their hosting nodes.
    /// </summary>
    public class RouterTable : IRouterTable
    {
        private readonly ConcurrentDictionary<string, NodeEndpoint> routes;

        /// <summary>
        /// Initializes a new instance of the <see cref="RouterTable"/> class.
        /// </summary>
        public RouterTable()
        {
            this.routes = new ConcurrentDictionary<string, NodeEndpoint>();
        }

        /// <inheritdoc/>
        public void AddRoute(string route, NodeEndpoint node)
        {
            this.routes[route] = node;
        }

        /// <inheritdoc/>
        public void RemoveRoute(string route)
        {
            this.routes.TryRemove(route, out _);
        }

        /// <inheritdoc/>
        public NodeEndpoint LookupRoute(string route)
        {
            return this.routes.TryGetValue(route, out var node) ? node : null;
        }

        /// <inheritdoc/>
        public IReadOnlyList<string> GetRoutesForNode(Guid nodeId)
        {
            return this.routes
                .Where(kvp => kvp.Value.NodeId == nodeId)
                .Select(kvp => kvp.Key)
                .ToList()
                .AsReadOnly();
        }

        /// <inheritdoc/>
        public void RemoveNode(Guid nodeId)
        {
            var keys = this.routes
                .Where(kvp => kvp.Value.NodeId == nodeId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keys)
            {
                this.routes.TryRemove(key, out _);
            }
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, NodeEndpoint> GetAll()
        {
            return new Dictionary<string, NodeEndpoint>(this.routes);
        }
    }
}
