// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GDictionary.cs" company="MLambda">
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
    /// A gossip-replicated dictionary using OR-Map semantics with last-writer-wins per key.
    /// Similar to GTree but without ordered iteration requirements.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    public class GDictionary<TKey, TValue> : ICrdtState<GDictionary<TKey, TValue>>
    {
        private readonly string stateId;
        private readonly string nodeId;
        private readonly Dictionary<TKey, GTreeEntry<TValue>> store;
        private long version;
        private long logicalClock;

        /// <summary>
        /// Initializes a new instance of the <see cref="GDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="stateId">The unique identifier for this CRDT instance.</param>
        /// <param name="nodeId">The node that owns this replica.</param>
        public GDictionary(string stateId, string nodeId)
        {
            this.stateId = stateId;
            this.nodeId = nodeId;
            this.store = new Dictionary<TKey, GTreeEntry<TValue>>();
            this.version = 0;
            this.logicalClock = 0;
        }

        private GDictionary(string stateId, string nodeId, Dictionary<TKey, GTreeEntry<TValue>> store, long version, long logicalClock)
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
        internal IReadOnlyDictionary<TKey, GTreeEntry<TValue>> AllEntries => this.store;

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
        /// Checks if a key exists and is not tombstoned.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists and is active.</returns>
        public bool ContainsKey(TKey key)
        {
            return this.store.TryGetValue(key, out var entry) && !entry.IsTombstone;
        }

        /// <summary>
        /// Returns all active key-value pairs.
        /// </summary>
        /// <returns>A list of active key-value pairs.</returns>
        public IReadOnlyList<KeyValuePair<TKey, TValue>> GetAll()
        {
            return this.store
                .Where(kvp => !kvp.Value.IsTombstone)
                .Select(kvp => new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value.Value))
                .ToList()
                .AsReadOnly();
        }

        /// <inheritdoc/>
        public GDictionary<TKey, TValue> Merge(GDictionary<TKey, TValue> other)
        {
            var merged = new Dictionary<TKey, GTreeEntry<TValue>>(this.store);
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
            return new GDictionary<TKey, TValue>(this.stateId, this.nodeId, merged, newVersion, maxClock);
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
