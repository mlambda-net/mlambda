// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RouteInfo.cs" company="MLambda">
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

namespace MLambda.Actors.Cluster.Abstraction
{
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Status of a route tracked by the RouteActor.
    /// </summary>
    public enum RouteStatus
    {
        /// <summary>
        /// The actor is running and accepting messages.
        /// </summary>
        Running,

        /// <summary>
        /// The actor is being created or waiting for a response.
        /// </summary>
        Waiting,

        /// <summary>
        /// The actor has been stopped gracefully.
        /// </summary>
        Stopped,

        /// <summary>
        /// The actor's satellite is unreachable or the actor has failed.
        /// </summary>
        Dead,
    }

    /// <summary>
    /// Information about a route tracked by the RouteActor.
    /// Stored in the gossip-replicated GTree.
    /// </summary>
    public class RouteInfo
    {
        /// <summary>
        /// Gets or sets the route name (e.g., actor path).
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// Gets or sets the satellite node that hosts this route's actor.
        /// </summary>
        public NodeEndpoint Satellite { get; set; }

        /// <summary>
        /// Gets or sets the current status of this route.
        /// </summary>
        public RouteStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the logical timestamp of the last update.
        /// </summary>
        public long LastUpdated { get; set; }
    }
}
