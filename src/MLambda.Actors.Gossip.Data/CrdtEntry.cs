// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CrdtEntry.cs" company="MLambda">
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
    using System;

    /// <summary>
    /// A single entry in a CRDT data structure with unique identity,
    /// causality tracking, and tombstone support.
    /// </summary>
    /// <typeparam name="T">The value type stored in this entry.</typeparam>
    public class CrdtEntry<T>
    {
        /// <summary>
        /// Gets or sets the globally unique identifier for this entry.
        /// </summary>
        public Guid UniqueId { get; set; }

        /// <summary>
        /// Gets or sets the value stored in this entry.
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Gets or sets the node that created this entry.
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Gets or sets the logical timestamp when this entry was created or last updated.
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this entry has been logically deleted.
        /// </summary>
        public bool IsTombstone { get; set; }

        /// <summary>
        /// Gets or sets the node that tombstoned this entry.
        /// </summary>
        public string TombstonedBy { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this entry was tombstoned.
        /// </summary>
        public long TombstoneTimestamp { get; set; }
    }
}
