// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GTree.cs" company="MLambda">
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
    /// A gossip-replicated ordered map using OR-Map semantics with last-writer-wins per key.
    /// Provides ordered iteration over keys via a sorted internal structure.
    /// </summary>
    /// <typeparam name="TKey">The key type. Must implement IComparable.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    public class GTree<TKey, TValue> : ICrdtState<GTree<TKey, TValue>>
        where TKey : IComparable<TKey>
    {
        private readonly string stateId;
        private readonly string nodeId;
        private readonly SortedDictionary<TKey, GTreeEntry<TValue>> store;
        private long version;
        private long logicalClock;

        /// <summary>
        /// Initializes a new instance of the <see cref="GTree{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="stateId">The unique identifier for this CRDT instance.</param>
        /// <param name="nodeId">The node that owns this replica.</param>
        public GTree(string stateId, string nodeId)
        {
            this.stateId = stateId;
            this.nodeId = nodeId;
            this.store = new SortedDictionary<TKey, GTreeEntry<TValue>>();
            this.version = 0;
            this.logicalClock = 0;
        }

        private GTree(string stateId, string nodeId, SortedDictionary<TKey, GTreeEntry<TValue>> store, long version, long logicalClock)
        {
            this.stateId = stateId;
            this.nodeId = nodeId;
            this.store = store;
            this.version = version;
            this.logicalClock = logicalClock;
        }

        /// <inheritdoc/>
        public string NodeId => this.nodeId;

        /// <summary>
        /// Gets the number of active (non-tombstoned) entries.
        /// </summary>
        public int Count => this.store.Values.Count(e => !e.IsTombstone);

        /// <summary>
        /// Gets all entries including tombstones, for replication.
        /// </summary>
        internal IReadOnlyDictionary<TKey, GTreeEntry<TValue>> AllEntries =>
            new Dictionary<TKey, GTreeEntry<TValue>>(this.store);

        /// <summary>
        /// Sets or updates a key-value pair using last-writer-wins semantics.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Set(TKey key, TValue value)
        {
            this.logicalClock++;
            this.store[key] = new GTreeEntry<TValue>
            {
                Value = value,
                NodeId = this.nodeId,
                Timestamp = this.logicalClock,
                IsTombstone = false,
            };

            this.version++;
        }

        /// <summary>
        /// Tries to get the value for a key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="value">The value if found.</param>
        /// <returns>True if the key exists and is not tombstoned.</returns>
        public bool TryGet(TKey key, out TValue value)
        {
            if (this.store.TryGetValue(key, out var entry) && !entry.IsTombstone)
            {
                value = entry.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Removes a key (tombstone).
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True if the key was found and tombstoned.</returns>
        public bool Remove(TKey key)
        {
            if (this.store.TryGetValue(key, out var entry) && !entry.IsTombstone)
            {
                this.logicalClock++;
                entry.IsTombstone = true;
                entry.TombstonedBy = this.nodeId;
                entry.TombstoneTimestamp = this.logicalClock;
                this.version++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns all active key-value pairs in sorted order.
        /// </summary>
        /// <returns>An ordered list of key-value pairs.</returns>
        public IReadOnlyList<KeyValuePair<TKey, TValue>> GetOrdered()
        {
            return this.store
                .Where(kvp => !kvp.Value.IsTombstone)
                .Select(kvp => new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value.Value))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Checks if a key exists and is not tombstoned.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists and is active.</returns>
        public bool ContainsKey(TKey key)
        {
            return this.store.TryGetValue(key, out var entry) && !entry.IsTombstone;
        }

        /// <inheritdoc/>
        public GTree<TKey, TValue> Merge(GTree<TKey, TValue> other)
        {
            var merged = new SortedDictionary<TKey, GTreeEntry<TValue>>(this.store);
            var maxClock = this.logicalClock;

            foreach (var kvp in other.store)
            {
                if (merged.TryGetValue(kvp.Key, out var existing))
                {
                    // LWW: higher timestamp wins.
                    var otherTs = kvp.Value.IsTombstone
                        ? kvp.Value.TombstoneTimestamp : kvp.Value.Timestamp;
                    var existingTs = existing.IsTombstone
                        ? existing.TombstoneTimestamp : existing.Timestamp;

                    if (otherTs > existingTs)
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
            return new GTree<TKey, TValue>(this.stateId, this.nodeId, merged, newVersion, maxClock);
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

    /// <summary>
    /// An entry in a GTree with LWW semantics and tombstone support.
    /// </summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    public class GTreeEntry<TValue>
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public TValue Value { get; set; }

        /// <summary>
        /// Gets or sets the node that last wrote this entry.
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Gets or sets the logical timestamp of the last write.
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this entry is tombstoned.
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
