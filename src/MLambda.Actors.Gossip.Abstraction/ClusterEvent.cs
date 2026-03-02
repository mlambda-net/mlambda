// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClusterEvent.cs" company="MLambda">
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

namespace MLambda.Actors.Gossip.Abstraction
{
    /// <summary>
    /// Base class for cluster membership events.
    /// </summary>
    public abstract class ClusterEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterEvent"/> class.
        /// </summary>
        /// <param name="member">The affected member.</param>
        protected ClusterEvent(Member member)
        {
            this.Member = member;
        }

        /// <summary>
        /// Gets the affected member.
        /// </summary>
        public Member Member { get; }
    }

    /// <summary>
    /// Event when a member joins the cluster.
    /// </summary>
    public class MemberJoined : ClusterEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberJoined"/> class.
        /// </summary>
        /// <param name="member">The joined member.</param>
        public MemberJoined(Member member)
            : base(member)
        {
        }
    }

    /// <summary>
    /// Event when a member transitions to Up status.
    /// </summary>
    public class MemberUp : ClusterEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberUp"/> class.
        /// </summary>
        /// <param name="member">The member.</param>
        public MemberUp(Member member)
            : base(member)
        {
        }
    }

    /// <summary>
    /// Event when a member leaves the cluster.
    /// </summary>
    public class MemberLeft : ClusterEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberLeft"/> class.
        /// </summary>
        /// <param name="member">The member.</param>
        public MemberLeft(Member member)
            : base(member)
        {
        }
    }

    /// <summary>
    /// Event when a member is suspected unreachable.
    /// </summary>
    public class MemberSuspected : ClusterEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberSuspected"/> class.
        /// </summary>
        /// <param name="member">The member.</param>
        public MemberSuspected(Member member)
            : base(member)
        {
        }
    }

    /// <summary>
    /// Event when a member is confirmed down.
    /// </summary>
    public class MemberDown : ClusterEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberDown"/> class.
        /// </summary>
        /// <param name="member">The member.</param>
        public MemberDown(Member member)
            : base(member)
        {
        }
    }

    /// <summary>
    /// Event when a member is removed from the cluster.
    /// </summary>
    public class MemberRemoved : ClusterEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberRemoved"/> class.
        /// </summary>
        /// <param name="member">The member.</param>
        public MemberRemoved(Member member)
            : base(member)
        {
        }
    }
}
