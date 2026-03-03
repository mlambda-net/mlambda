// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ICrdtState.cs" company="MLambda">
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
    /// <summary>
    /// Base interface for conflict-free replicated data types (CRDTs).
    /// </summary>
    /// <typeparam name="T">The concrete CRDT state type.</typeparam>
    public interface ICrdtState<T>
        where T : ICrdtState<T>
    {
        /// <summary>
        /// Gets the node ID that owns this local replica.
        /// </summary>
        string NodeId { get; }

        /// <summary>
        /// Merges this state with a remote state, producing a converged result.
        /// </summary>
        /// <param name="other">The remote state to merge with.</param>
        /// <returns>The merged state.</returns>
        T Merge(T other);

        /// <summary>
        /// Returns a compact digest for comparison during gossip sync rounds.
        /// </summary>
        /// <returns>A digest summarizing the current state.</returns>
        CrdtDigest GetDigest();
    }

    /// <summary>
    /// A compact summary of CRDT state for efficient gossip comparison.
    /// </summary>
    public class CrdtDigest
    {
        /// <summary>
        /// Gets or sets the unique identifier for this CRDT instance.
        /// </summary>
        public string StateId { get; set; }

        /// <summary>
        /// Gets or sets the version counter for change detection.
        /// </summary>
        public long Version { get; set; }

        /// <summary>
        /// Gets or sets the number of active (non-tombstoned) entries.
        /// </summary>
        public int EntryCount { get; set; }
    }
}
