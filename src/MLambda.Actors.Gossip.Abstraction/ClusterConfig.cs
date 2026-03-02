// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClusterConfig.cs" company="MLambda">
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
    /// Configuration for the cluster gossip protocol.
    /// </summary>
    public class ClusterConfig
    {
        /// <summary>
        /// Gets or sets the local endpoint.
        /// </summary>
        public NodeEndpoint LocalEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the seed nodes for cluster discovery.
        /// </summary>
        public List<NodeEndpoint> SeedNodes { get; set; } = new List<NodeEndpoint>();

        /// <summary>
        /// Gets or sets the gossip interval. Default 1 second.
        /// </summary>
        public TimeSpan GossipInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the heartbeat interval. Default 500ms.
        /// </summary>
        public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets or sets the phi threshold for failure detection. Default 8.0.
        /// </summary>
        public double PhiThreshold { get; set; } = 8.0;

        /// <summary>
        /// Gets or sets the suspect timeout. Default 30 seconds.
        /// </summary>
        public TimeSpan SuspectTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the gossip fanout count. Default 3.
        /// </summary>
        public int GossipFanout { get; set; } = 3;
    }
}
