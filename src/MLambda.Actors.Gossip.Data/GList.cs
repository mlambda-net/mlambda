// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GList.cs" company="MLambda">
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
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A gossip-replicated list using OR-Set semantics.
    /// Elements are uniquely identified and can be added/removed with convergent merge.
    /// </summary>
    /// <typeparam name="T">The type of values stored in the list.</typeparam>
    public class GList<T> : ICrdtState<GList<T>>
    {
        private readonly string stateId;
        private readonly string nodeId;
        private readonly Dictionary<Guid, CrdtEntry<T>> entries;
        private long version;
        private long logicalClock;

        /// <summary>
        /// Initializes a new instance of the <see cref="GList{T}"/> class.
        /// </summary>
        /// <param name="stateId">The unique identifier for this CRDT instance.</param>
        /// <param name="nodeId">The node that owns this replica.</param>
        public GList(string stateId, string nodeId)
        {
            this.stateId = stateId;
            this.nodeId = nodeId;
            this.entries = new Dictionary<Guid, CrdtEntry<T>>();
            this.version = 0;
            this.logicalClock = 0;
        }

        private GList(string stateId, string nodeId, Dictionary<Guid, CrdtEntry<T>> entries, long version, long logicalClock)
        {
            this.stateId = stateId;
            this.nodeId = nodeId;
            this.entries = entries;
            this.version = version;
            this.logicalClock = logicalClock;
        }

        /// <inheritdoc/>
        public string NodeId => this.nodeId;

        /// <summary>
        /// Gets all non-tombstoned entries ordered by timestamp.
        /// </summary>
        public IReadOnlyList<CrdtEntry<T>> Items =>
            this.entries.Values
                .Where(e => !e.IsTombstone)
                .OrderBy(e => e.Timestamp)
                .ThenBy(e => e.UniqueId)
                .ToList()
                .AsReadOnly();

        /// <summary>
        /// Gets the number of non-tombstoned entries.
        /// </summary>
        public int Count => this.entries.Values.Count(e => !e.IsTombstone);

        /// <summary>
        /// Gets all entries including tombstones, for replication.
        /// </summary>
        internal IReadOnlyDictionary<Guid, CrdtEntry<T>> AllEntries => this.entries;

        /// <summary>
        /// Adds a value to the list.
        /// </summary>
        /// <param name="item">The value to add.</param>
        /// <returns>The unique ID of the added entry.</returns>
        public Guid Add(T item)
        {
            this.logicalClock++;
            var entry = new CrdtEntry<T>
            {
                UniqueId = Guid.NewGuid(),
                Value = item,
                NodeId = this.nodeId,
                Timestamp = this.logicalClock,
                IsTombstone = false,
            };

            this.entries[entry.UniqueId] = entry;
            this.version++;
            return entry.UniqueId;
        }

        /// <summary>
        /// Removes an entry by its unique ID (tombstone).
        /// </summary>
        /// <param name="entryId">The unique ID of the entry to remove.</param>
        /// <returns>True if the entry was found and tombstoned.</returns>
        public bool Remove(Guid entryId)
        {
            if (this.entries.TryGetValue(entryId, out var entry) && !entry.IsTombstone)
            {
                entry.IsTombstone = true;
                entry.TombstonedBy = this.nodeId;
                entry.TombstoneTimestamp = ++this.logicalClock;
                this.version++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a value exists in the list (non-tombstoned).
        /// </summary>
        /// <param name="entryId">The entry ID to check.</param>
        /// <returns>True if the entry exists and is not tombstoned.</returns>
        public bool Contains(Guid entryId)
        {
            return this.entries.TryGetValue(entryId, out var entry) && !entry.IsTombstone;
        }

        /// <summary>
        /// Gets an entry by its unique ID.
        /// </summary>
        /// <param name="entryId">The entry ID.</param>
        /// <param name="entry">The entry if found.</param>
        /// <returns>True if the entry exists and is not tombstoned.</returns>
        public bool TryGet(Guid entryId, out CrdtEntry<T> entry)
        {
            if (this.entries.TryGetValue(entryId, out entry) && !entry.IsTombstone)
            {
                return true;
            }

            entry = null;
            return false;
        }

        /// <inheritdoc/>
        public GList<T> Merge(GList<T> other)
        {
            var merged = new Dictionary<Guid, CrdtEntry<T>>(this.entries);
            var maxClock = this.logicalClock;

            foreach (var kvp in other.entries)
            {
                if (merged.TryGetValue(kvp.Key, out var existing))
                {
                    if (kvp.Value.IsTombstone && !existing.IsTombstone)
                    {
                        merged[kvp.Key] = kvp.Value;
                    }
                    else if (kvp.Value.IsTombstone && existing.IsTombstone
                        && kvp.Value.TombstoneTimestamp > existing.TombstoneTimestamp)
                    {
                        merged[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    merged[kvp.Key] = kvp.Value;
                }

                if (kvp.Value.Timestamp > maxClock)
                {
                    maxClock = kvp.Value.Timestamp;
                }

                if (kvp.Value.TombstoneTimestamp > maxClock)
                {
                    maxClock = kvp.Value.TombstoneTimestamp;
                }
            }

            var newVersion = Math.Max(this.version, other.version) + 1;
            return new GList<T>(this.stateId, this.nodeId, merged, newVersion, maxClock);
        }

        /// <inheritdoc/>
        public CrdtDigest GetDigest()
        {
            return new CrdtDigest
            {
                StateId = this.stateId,
                Version = this.version,
                EntryCount = this.Count,
            };
        }
    }
}
