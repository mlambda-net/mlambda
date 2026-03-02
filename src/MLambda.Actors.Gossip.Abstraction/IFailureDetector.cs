// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IFailureDetector.cs" company="MLambda">
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
    using System;

    /// <summary>
    /// Detects node failures using heartbeat analysis.
    /// </summary>
    public interface IFailureDetector
    {
        /// <summary>
        /// Records a heartbeat from a node.
        /// </summary>
        /// <param name="nodeId">The node identifier.</param>
        void Heartbeat(Guid nodeId);

        /// <summary>
        /// Checks if a node is considered available.
        /// </summary>
        /// <param name="nodeId">The node identifier.</param>
        /// <returns>True if available.</returns>
        bool IsAvailable(Guid nodeId);

        /// <summary>
        /// Gets the phi (suspicion) level for a node.
        /// </summary>
        /// <param name="nodeId">The node identifier.</param>
        /// <returns>The phi value.</returns>
        double GetSuspicionLevel(Guid nodeId);
    }
}
