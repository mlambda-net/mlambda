// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IStatefulActor.cs" company="MLambda">
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
    /// Optional interface for actors that support state persistence and restoration.
    /// When an actor implementing this interface is re-created on failover,
    /// its state is restored from the last saved snapshot.
    /// </summary>
    public interface IStatefulActor
    {
        /// <summary>
        /// Captures the current state of the actor for persistence.
        /// </summary>
        /// <returns>The serializable state object.</returns>
        object GetState();

        /// <summary>
        /// Restores the actor state from a previously saved snapshot.
        /// Called after failover re-creation.
        /// </summary>
        /// <param name="state">The state object to restore.</param>
        void RestoreState(object state);
    }
}
