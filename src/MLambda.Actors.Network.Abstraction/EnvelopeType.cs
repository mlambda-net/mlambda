// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnvelopeType.cs" company="MLambda">
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

namespace MLambda.Actors.Network.Abstraction
{
    /// <summary>
    /// The type of network envelope message.
    /// </summary>
    public enum EnvelopeType
    {
        /// <summary>
        /// Fire-and-forget message.
        /// </summary>
        Tell,

        /// <summary>
        /// Request-response message.
        /// </summary>
        Ask,

        /// <summary>
        /// Response to an ask message.
        /// </summary>
        Response,

        /// <summary>
        /// System-level message (gossip, cluster management).
        /// </summary>
        SystemMessage,

        /// <summary>
        /// Topology message for satellite registration, dispatch, and work results.
        /// </summary>
        Topology,

        /// <summary>
        /// CRDT state synchronization message for gossip data replication.
        /// </summary>
        CrdtSync,
    }
}
