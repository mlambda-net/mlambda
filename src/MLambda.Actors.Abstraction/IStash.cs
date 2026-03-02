// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IStash.cs" company="MLambda">
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
    /// The stash interface for storing messages temporarily.
    /// </summary>
    public interface IStash
    {
        /// <summary>
        /// Stashes the current message.
        /// </summary>
        void Stash();

        /// <summary>
        /// Unstashes the most recently stashed message.
        /// </summary>
        void Unstash();

        /// <summary>
        /// Unstashes all stashed messages.
        /// </summary>
        void UnstashAll();
    }
}
