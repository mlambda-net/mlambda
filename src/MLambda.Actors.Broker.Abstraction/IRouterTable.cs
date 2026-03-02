// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IRouterTable.cs" company="MLambda">
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

namespace MLambda.Actors.Broker.Abstraction
{
    using System;
    using System.Collections.Generic;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Maintains a routing table that maps actor routes to their hosting nodes.
    /// </summary>
    public interface IRouterTable
    {
        /// <summary>
        /// Adds or updates a route entry.
        /// </summary>
        /// <param name="route">The actor route path.</param>
        /// <param name="node">The node hosting the actor.</param>
        void AddRoute(string route, NodeEndpoint node);

        /// <summary>
        /// Removes a route entry.
        /// </summary>
        /// <param name="route">The actor route path.</param>
        void RemoveRoute(string route);

        /// <summary>
        /// Looks up the node for a given route.
        /// </summary>
        /// <param name="route">The actor route path.</param>
        /// <returns>The node endpoint, or null if not found.</returns>
        NodeEndpoint LookupRoute(string route);

        /// <summary>
        /// Gets all routes hosted on a specific node.
        /// </summary>
        /// <param name="nodeId">The node identifier.</param>
        /// <returns>The routes hosted on the node.</returns>
        IReadOnlyList<string> GetRoutesForNode(Guid nodeId);

        /// <summary>
        /// Removes all routes for a given node.
        /// </summary>
        /// <param name="nodeId">The node identifier.</param>
        void RemoveNode(Guid nodeId);

        /// <summary>
        /// Gets all route entries.
        /// </summary>
        /// <returns>A read-only dictionary of all routes.</returns>
        IReadOnlyDictionary<string, NodeEndpoint> GetAll();
    }
}
