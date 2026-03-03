// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GossipDataMessages.cs" company="MLambda">
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

namespace MLambda.Actors.Gossip.Data
{
    using System.Collections.Generic;

    /// <summary>
    /// Sent during a gossip round to exchange CRDT digests with a peer.
    /// The receiver compares digests to determine which states need synchronization.
    /// </summary>
    public class CrdtSyncRequest
    {
        /// <summary>
        /// Gets or sets the node ID of the sender.
        /// </summary>
        public string SenderId { get; set; }

        /// <summary>
        /// Gets or sets the digests of all locally registered CRDT states.
        /// </summary>
        public List<CrdtDigest> Digests { get; set; } = new List<CrdtDigest>();
    }

    /// <summary>
    /// Response to a <see cref="CrdtSyncRequest"/> containing full CRDT state payloads
    /// for states that the requester is behind on.
    /// </summary>
    public class CrdtSyncResponse
    {
        /// <summary>
        /// Gets or sets the node ID of the sender.
        /// </summary>
        public string SenderId { get; set; }

        /// <summary>
        /// Gets or sets the collection of CRDT state payloads to merge.
        /// Each entry contains the serialized state and its type information.
        /// </summary>
        public List<CrdtStatePayload> States { get; set; } = new List<CrdtStatePayload>();

        /// <summary>
        /// Gets or sets the digests that the sender is requesting from the receiver.
        /// The receiver should respond with the full state for these.
        /// </summary>
        public List<CrdtDigest> RequestedDigests { get; set; } = new List<CrdtDigest>();
    }

    /// <summary>
    /// A serialized CRDT state payload for transport.
    /// </summary>
    public class CrdtStatePayload
    {
        /// <summary>
        /// Gets or sets the unique state identifier.
        /// </summary>
        public string StateId { get; set; }

        /// <summary>
        /// Gets or sets the serialized type name for deserialization.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Gets or sets the serialized state bytes.
        /// </summary>
        public byte[] Data { get; set; }
    }
}
