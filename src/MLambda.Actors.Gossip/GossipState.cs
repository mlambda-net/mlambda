// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GossipState.cs" company="MLambda">
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

namespace MLambda.Actors.Gossip
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MLambda.Actors.Gossip.Abstraction;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Immutable gossip state that can be merged with remote states.
    /// </summary>
    public class GossipState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GossipState"/> class.
        /// </summary>
        public GossipState()
        {
            this.Members = new Dictionary<Guid, Member>();
            this.Version = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GossipState"/> class.
        /// </summary>
        /// <param name="members">The members dictionary.</param>
        /// <param name="version">The state version.</param>
        public GossipState(Dictionary<Guid, Member> members, long version)
        {
            this.Members = new Dictionary<Guid, Member>(members);
            this.Version = version;
        }

        /// <summary>
        /// Gets the members dictionary.
        /// </summary>
        public Dictionary<Guid, Member> Members { get; }

        /// <summary>
        /// Gets the state version.
        /// </summary>
        public long Version { get; }

        /// <summary>
        /// Merges this state with another using last-writer-wins per member.
        /// </summary>
        /// <param name="other">The other state to merge with.</param>
        /// <returns>The merged state and any changed members.</returns>
        public (GossipState State, List<(Member Member, MemberStatus OldStatus)> Changes) Merge(GossipState other)
        {
            var merged = new Dictionary<Guid, Member>(this.Members);
            var changes = new List<(Member Member, MemberStatus OldStatus)>();

            foreach (var kvp in other.Members)
            {
                if (merged.TryGetValue(kvp.Key, out var existing))
                {
                    if (kvp.Value.HeartbeatSequence > existing.HeartbeatSequence)
                    {
                        var oldStatus = existing.Status;
                        merged[kvp.Key] = kvp.Value;
                        if (kvp.Value.Status != oldStatus)
                        {
                            changes.Add((kvp.Value, oldStatus));
                        }
                    }
                }
                else
                {
                    merged[kvp.Key] = kvp.Value;
                    changes.Add((kvp.Value, MemberStatus.Removed));
                }
            }

            var newVersion = Math.Max(this.Version, other.Version) + 1;
            return (new GossipState(merged, newVersion), changes);
        }

        /// <summary>
        /// Adds or updates a member in the state.
        /// </summary>
        /// <param name="member">The member to set.</param>
        /// <returns>A new state with the member.</returns>
        public GossipState SetMember(Member member)
        {
            var members = new Dictionary<Guid, Member>(this.Members);
            members[member.Endpoint.NodeId] = member;
            return new GossipState(members, this.Version + 1);
        }
    }
}
