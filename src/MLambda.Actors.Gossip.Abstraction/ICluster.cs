// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ICluster.cs" company="MLambda">
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

namespace MLambda.Actors.Gossip.Abstraction
{
    using System;
    using System.Collections.Generic;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// The cluster membership interface.
    /// </summary>
    public interface ICluster
    {
        /// <summary>
        /// Gets the local node endpoint.
        /// </summary>
        NodeEndpoint Self { get; }

        /// <summary>
        /// Gets the stream of cluster events.
        /// </summary>
        IObservable<ClusterEvent> Events { get; }

        /// <summary>
        /// Gets the current cluster members.
        /// </summary>
        IReadOnlyCollection<Member> Members { get; }

        /// <summary>
        /// Gets a member by node identifier.
        /// </summary>
        /// <param name="nodeId">The node identifier.</param>
        /// <returns>The member, or null.</returns>
        Member GetMember(Guid nodeId);
    }
}
