// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BrokerMessages.cs" company="MLambda">
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
    /// Message to register a local actor route with the broker.
    /// </summary>
    public class RegisterRoute
    {
        /// <summary>
        /// Gets or sets the actor route path.
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// Gets or sets the actor identifier.
        /// </summary>
        public Guid ActorId { get; set; }
    }

    /// <summary>
    /// Message to unregister an actor route from the broker.
    /// </summary>
    public class UnregisterRoute
    {
        /// <summary>
        /// Gets or sets the actor route path.
        /// </summary>
        public string Route { get; set; }
    }

    /// <summary>
    /// Message to look up which node hosts a given route.
    /// </summary>
    public class LookupRoute
    {
        /// <summary>
        /// Gets or sets the actor route path to look up.
        /// </summary>
        public string Route { get; set; }
    }

    /// <summary>
    /// Response containing the result of a route lookup.
    /// </summary>
    public class LookupRouteResult
    {
        /// <summary>
        /// Gets or sets the route that was looked up.
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// Gets or sets the node hosting the route, or null if not found.
        /// </summary>
        public NodeEndpoint Node { get; set; }
    }

    /// <summary>
    /// Message to announce local routes to cluster peers.
    /// </summary>
    public class AnnounceRoutes
    {
        /// <summary>
        /// Gets or sets the source node.
        /// </summary>
        public NodeEndpoint SourceNode { get; set; }

        /// <summary>
        /// Gets or sets the routes being announced.
        /// </summary>
        public IReadOnlyList<string> Routes { get; set; }
    }

    /// <summary>
    /// Message to request route discovery from peers.
    /// </summary>
    public class DiscoverRoutes
    {
    }

    /// <summary>
    /// Response containing all known routes.
    /// </summary>
    public class DiscoverRoutesResult
    {
        /// <summary>
        /// Gets or sets the known routes and their nodes.
        /// </summary>
        public IReadOnlyDictionary<string, NodeEndpoint> Routes { get; set; }
    }

    /// <summary>
    /// Message indicating a node has left the cluster.
    /// </summary>
    public class NodeLeft
    {
        /// <summary>
        /// Gets or sets the identifier of the node that left.
        /// </summary>
        public Guid NodeId { get; set; }
    }
}
