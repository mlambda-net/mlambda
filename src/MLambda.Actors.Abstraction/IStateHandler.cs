// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IStateHandler.cs" company="MLambda">
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
    using System.Threading.Tasks;

    /// <summary>
    /// Provides persistence for actor state snapshots.
    /// Injected into actors that opt in to the HandleState pattern.
    /// </summary>
    /// <typeparam name="TState">The type of the actor state.</typeparam>
    public interface IStateHandler<TState>
    {
        /// <summary>
        /// Persists the current actor state snapshot.
        /// Called when the actor returns <see cref="StateDecision.Flush"/>.
        /// </summary>
        /// <param name="state">The state to persist.</param>
        /// <returns>A task representing the async save operation.</returns>
        Task Save(TState state);

        /// <summary>
        /// Retrieves the most recently persisted actor state snapshot.
        /// Called during actor startup to restore state.
        /// </summary>
        /// <returns>The persisted state, or default if none exists.</returns>
        Task<TState> Get();
    }
}
