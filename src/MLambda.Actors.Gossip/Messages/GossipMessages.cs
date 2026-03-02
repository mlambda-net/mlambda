// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GossipMessages.cs" company="MLambda">
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

namespace MLambda.Actors.Gossip.Messages
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A digest entry for gossip state comparison.
    /// </summary>
    public class GossipDigest
    {
        /// <summary>
        /// Gets or sets the node identifier.
        /// </summary>
        public Guid NodeId { get; set; }

        /// <summary>
        /// Gets or sets the heartbeat sequence.
        /// </summary>
        public long HeartbeatSequence { get; set; }
    }

    /// <summary>
    /// Gossip SYN message - sent with state digests.
    /// </summary>
    public class GossipSyn
    {
        /// <summary>
        /// Gets or sets the sender node identifier.
        /// </summary>
        public Guid SenderId { get; set; }

        /// <summary>
        /// Gets or sets the state digests.
        /// </summary>
        public List<GossipDigest> Digests { get; set; } = new List<GossipDigest>();
    }

    /// <summary>
    /// Gossip ACK message - responds with full state for stale entries.
    /// </summary>
    public class GossipAck
    {
        /// <summary>
        /// Gets or sets the sender node identifier.
        /// </summary>
        public Guid SenderId { get; set; }

        /// <summary>
        /// Gets or sets the updated member states.
        /// </summary>
        public List<GossipMemberState> UpdatedMembers { get; set; } = new List<GossipMemberState>();

        /// <summary>
        /// Gets or sets the digests the sender needs from us.
        /// </summary>
        public List<GossipDigest> RequestedDigests { get; set; } = new List<GossipDigest>();
    }

    /// <summary>
    /// Gossip ACK2 message - final phase with requested entries.
    /// </summary>
    public class GossipAck2
    {
        /// <summary>
        /// Gets or sets the sender node identifier.
        /// </summary>
        public Guid SenderId { get; set; }

        /// <summary>
        /// Gets or sets the requested member states.
        /// </summary>
        public List<GossipMemberState> Members { get; set; } = new List<GossipMemberState>();
    }

    /// <summary>
    /// Serializable member state for gossip exchange.
    /// </summary>
    public class GossipMemberState
    {
        /// <summary>
        /// Gets or sets the node identifier.
        /// </summary>
        public Guid NodeId { get; set; }

        /// <summary>
        /// Gets or sets the host.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Gets or sets the heartbeat sequence.
        /// </summary>
        public long HeartbeatSequence { get; set; }

        /// <summary>
        /// Gets or sets the last seen ticks.
        /// </summary>
        public long LastSeenTicks { get; set; }
    }

    /// <summary>
    /// Join request message sent to seed nodes.
    /// </summary>
    public class JoinRequest
    {
        /// <summary>
        /// Gets or sets the joining node identifier.
        /// </summary>
        public Guid NodeId { get; set; }

        /// <summary>
        /// Gets or sets the joining node host.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the joining node port.
        /// </summary>
        public int Port { get; set; }
    }

    /// <summary>
    /// Leave request message.
    /// </summary>
    public class LeaveRequest
    {
        /// <summary>
        /// Gets or sets the leaving node identifier.
        /// </summary>
        public Guid NodeId { get; set; }
    }
}
