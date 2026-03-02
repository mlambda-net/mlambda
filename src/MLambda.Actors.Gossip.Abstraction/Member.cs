// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Member.cs" company="MLambda">
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
    /// Represents a member in the cluster.
    /// </summary>
    public class Member
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Member"/> class.
        /// </summary>
        /// <param name="endpoint">The member endpoint.</param>
        /// <param name="status">The member status.</param>
        public Member(NodeEndpoint endpoint, MemberStatus status)
        {
            this.Endpoint = endpoint;
            this.Status = status;
            this.HeartbeatSequence = 0;
            this.LastSeen = DateTimeOffset.UtcNow;
            this.Metadata = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets the member endpoint.
        /// </summary>
        public NodeEndpoint Endpoint { get; }

        /// <summary>
        /// Gets or sets the member status.
        /// </summary>
        public MemberStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the heartbeat sequence number.
        /// </summary>
        public long HeartbeatSequence { get; set; }

        /// <summary>
        /// Gets or sets the last seen timestamp.
        /// </summary>
        public DateTimeOffset LastSeen { get; set; }

        /// <summary>
        /// Gets the metadata dictionary.
        /// </summary>
        public IReadOnlyDictionary<string, string> Metadata { get; init; }
    }
}
