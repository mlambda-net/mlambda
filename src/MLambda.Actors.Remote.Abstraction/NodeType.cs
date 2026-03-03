// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NodeType.cs" company="MLambda">
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
    /// <summary>
    /// Defines the role of a node in the hub-spoke cluster topology.
    /// </summary>
    public enum NodeType
    {
        /// <summary>
        /// A cluster node that participates in the gossip mesh, manages routing,
        /// and dispatches work to satellite nodes. Does not run user actors.
        /// </summary>
        Cluster,

        /// <summary>
        /// A satellite node that connects to cluster nodes and executes user actors.
        /// Does not participate in the gossip mesh.
        /// </summary>
        Satellite,

        /// <summary>
        /// A hybrid node that participates in the gossip cluster AND runs user actors.
        /// Acts as both a cluster coordinator and a satellite worker.
        /// </summary>
        Hybrid,
    }
}
