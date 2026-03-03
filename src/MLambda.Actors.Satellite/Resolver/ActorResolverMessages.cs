// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActorResolverMessages.cs" company="MLambda">
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

namespace MLambda.Actors.Satellite.Resolver
{
    /// <summary>
    /// Message sent when a node leaves the cluster, triggering failover.
    /// </summary>
    public class ResolverNodeLeft
    {
        /// <summary>
        /// Gets or sets the identifier of the node that left.
        /// </summary>
        public string NodeId { get; set; }
    }

    /// <summary>
    /// Message to request restoration of orphaned routes from a dead node.
    /// </summary>
    public class RestoreOrphanedRoutes
    {
        /// <summary>
        /// Gets or sets the identifier of the dead node.
        /// </summary>
        public string DeadNodeId { get; set; }
    }
}
