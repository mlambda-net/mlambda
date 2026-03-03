// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StateResult.cs" company="MLambda">
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
    /// Wraps an actor's response value together with a <see cref="StateDecision"/>
    /// that controls message retention and state persistence.
    /// Actors return this via <see cref="Actor.Flush"/> or <see cref="Actor.Keep"/>
    /// to opt in to the HandleState pattern.
    /// </summary>
    public class StateResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateResult"/> class.
        /// </summary>
        /// <param name="value">The response value.</param>
        /// <param name="decision">The state decision.</param>
        public StateResult(object value, StateDecision decision)
        {
            this.Value = value;
            this.Decision = decision;
        }

        /// <summary>
        /// Gets the response value to forward to the caller.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Gets the state decision indicating whether the message should be
        /// flushed from persistent storage or kept for retry.
        /// </summary>
        public StateDecision Decision { get; }
    }
}
