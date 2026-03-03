// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GStack.cs" company="MLambda">
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
    /// A gossip-replicated FIFO queue using OR-Set semantics.
    /// Push adds entries with unique IDs and timestamps.
    /// Pop consumes the oldest non-tombstoned entry (FIFO order).
    /// Merge produces the union of adds and tombstones, ensuring convergence.
    /// </summary>
    /// <typeparam name="T">The type of values stored in the stack.</typeparam>
    public class GStack<T> : ICrdtState<GStack<T>>
    {
        private readonly string stateId;
        private readonly string nodeId;
        private readonly Dictionary<Guid, CrdtEntry<T>> entries;
        private long version;
        private long logicalClock;

        /// <summary>
        /// Initializes a new instance of the <see cref="GStack{T}"/> class.
        /// </summary>
        /// <param name="stateId">The unique identifier for this CRDT instance.</param>
        /// <param name="nodeId">The node that owns this replica.</param>
        public GStack(string stateId, string nodeId)
        {
            this.stateId = stateId;
            this.nodeId = nodeId;
            this.entries = new Dictionary<Guid, CrdtEntry<T>>();
            this.version = 0;
            this.logicalClock = 0;
        }

        private GStack(string stateId, string nodeId, Dictionary<Guid, CrdtEntry<T>> entries, long version, long logicalClock)
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
        /// Gets the number of non-tombstoned entries.
        /// </summary>
        public int Count => this.entries.Values.Count(e => !e.IsTombstone);

        /// <summary>
        /// Gets all entries including tombstones, for replication.
        /// </summary>
        internal IReadOnlyDictionary<Guid, CrdtEntry<T>> AllEntries => this.entries;

        /// <summary>
        /// Pushes a value onto the queue.
        /// </summary>
        /// <param name="item">The value to push.</param>
        public void Push(T item)
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
        }

        /// <summary>
        /// Pops the oldest non-tombstoned entry (FIFO order).
        /// </summary>
        /// <returns>The consumed entry, or null if the queue is empty.</returns>
        public CrdtEntry<T> Pop()
        {
            var oldest = this.entries.Values
                .Where(e => !e.IsTombstone)
                .OrderBy(e => e.Timestamp)
                .ThenBy(e => e.UniqueId)
                .FirstOrDefault();

            if (oldest == null)
            {
                return null;
            }

            oldest.IsTombstone = true;
            oldest.TombstonedBy = this.nodeId;
            oldest.TombstoneTimestamp = ++this.logicalClock;
            this.version++;
            return oldest;
        }

        /// <summary>
        /// Returns all non-tombstoned entries ordered by timestamp (FIFO).
        /// </summary>
        /// <returns>A read-only list of active entries.</returns>
        public IReadOnlyList<CrdtEntry<T>> PeekAll()
        {
            return this.entries.Values
                .Where(e => !e.IsTombstone)
                .OrderBy(e => e.Timestamp)
                .ThenBy(e => e.UniqueId)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Peeks at the oldest non-tombstoned entry without consuming it.
        /// </summary>
        /// <returns>The oldest entry, or null if empty.</returns>
        public CrdtEntry<T> Peek()
        {
            return this.entries.Values
                .Where(e => !e.IsTombstone)
                .OrderBy(e => e.Timestamp)
                .ThenBy(e => e.UniqueId)
                .FirstOrDefault();
        }

        /// <inheritdoc/>
        public GStack<T> Merge(GStack<T> other)
        {
            var merged = new Dictionary<Guid, CrdtEntry<T>>(this.entries);
            var maxClock = this.logicalClock;

            foreach (var kvp in other.entries)
            {
                if (merged.TryGetValue(kvp.Key, out var existing))
                {
                    // If the remote entry is tombstoned and local is not, apply tombstone.
                    // If both are tombstoned, keep the one with higher tombstone timestamp.
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
            return new GStack<T>(this.stateId, this.nodeId, merged, newVersion, maxClock);
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
