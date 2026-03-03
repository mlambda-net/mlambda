// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VectorClock.cs" company="MLambda">
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
    /// A vector clock for tracking causal ordering across distributed nodes.
    /// </summary>
    public class VectorClock
    {
        private readonly Dictionary<string, long> clocks;

        /// <summary>
        /// Initializes a new instance of the <see cref="VectorClock"/> class.
        /// </summary>
        public VectorClock()
        {
            this.clocks = new Dictionary<string, long>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VectorClock"/> class
        /// from an existing clock dictionary.
        /// </summary>
        /// <param name="clocks">The initial clock values.</param>
        public VectorClock(Dictionary<string, long> clocks)
        {
            this.clocks = new Dictionary<string, long>(clocks);
        }

        /// <summary>
        /// Gets the clock values as a read-only dictionary.
        /// </summary>
        public IReadOnlyDictionary<string, long> Clocks => this.clocks;

        /// <summary>
        /// Increments the clock for the given node and returns a new vector clock.
        /// </summary>
        /// <param name="nodeId">The node to increment.</param>
        /// <returns>A new vector clock with the incremented value.</returns>
        public VectorClock Increment(string nodeId)
        {
            var newClocks = new Dictionary<string, long>(this.clocks);
            newClocks[nodeId] = newClocks.GetValueOrDefault(nodeId, 0) + 1;
            return new VectorClock(newClocks);
        }

        /// <summary>
        /// Merges this vector clock with another, taking the maximum of each entry.
        /// </summary>
        /// <param name="other">The other vector clock.</param>
        /// <returns>A new merged vector clock.</returns>
        public VectorClock Merge(VectorClock other)
        {
            var merged = new Dictionary<string, long>(this.clocks);
            foreach (var kvp in other.clocks)
            {
                merged[kvp.Key] = Math.Max(merged.GetValueOrDefault(kvp.Key, 0), kvp.Value);
            }

            return new VectorClock(merged);
        }

        /// <summary>
        /// Returns true if this clock is strictly before the other (all entries &lt;= and at least one &lt;).
        /// </summary>
        /// <param name="other">The other vector clock.</param>
        /// <returns>True if this clock is causally before the other.</returns>
        public bool IsBefore(VectorClock other)
        {
            var allKeys = this.clocks.Keys.Union(other.clocks.Keys);
            bool atLeastOneLess = false;

            foreach (var key in allKeys)
            {
                var thisVal = this.clocks.GetValueOrDefault(key, 0);
                var otherVal = other.clocks.GetValueOrDefault(key, 0);

                if (thisVal > otherVal)
                {
                    return false;
                }

                if (thisVal < otherVal)
                {
                    atLeastOneLess = true;
                }
            }

            return atLeastOneLess;
        }

        /// <summary>
        /// Returns true if this clock and the other are concurrent (neither is before the other).
        /// </summary>
        /// <param name="other">The other vector clock.</param>
        /// <returns>True if the clocks are concurrent.</returns>
        public bool IsConcurrent(VectorClock other)
        {
            return !this.IsBefore(other) && !other.IsBefore(this) && !this.Equals(other);
        }

        /// <summary>
        /// Returns the sum of all clock entries, useful as a simple version number.
        /// </summary>
        /// <returns>The sum of all clock values.</returns>
        public long Sum() => this.clocks.Values.Sum();

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is not VectorClock other)
            {
                return false;
            }

            var allKeys = this.clocks.Keys.Union(other.clocks.Keys);
            return allKeys.All(k =>
                this.clocks.GetValueOrDefault(k, 0) == other.clocks.GetValueOrDefault(k, 0));
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = 17;
            foreach (var kvp in this.clocks.OrderBy(k => k.Key))
            {
                hash = (hash * 31) + kvp.Key.GetHashCode();
                hash = (hash * 31) + kvp.Value.GetHashCode();
            }

            return hash;
        }
    }
}
