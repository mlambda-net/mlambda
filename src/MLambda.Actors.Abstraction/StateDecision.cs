// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StateDecision.cs" company="MLambda">
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

namespace MLambda.Actors.Abstraction
{
    /// <summary>
    /// Represents the actor's decision about state handling after processing a message.
    /// </summary>
    public enum StateDecision
    {
        /// <summary>
        /// Message processed successfully. Remove from persistent storage and
        /// save the current actor state snapshot.
        /// </summary>
        Flush,

        /// <summary>
        /// Message should be retried. Keep in persistent storage until
        /// a subsequent attempt returns <see cref="Flush"/>.
        /// </summary>
        Keep,
    }
}
