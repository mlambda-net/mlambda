// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetMood.cs" company="MLambda">
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

namespace MLambda.Actors.Test.Actors.Command
{
    /// <summary>
    /// Command to set an actor's mood for Become/Unbecome testing.
    /// </summary>
    public class SetMood
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetMood"/> class.
        /// </summary>
        /// <param name="mood">The mood value.</param>
        public SetMood(string mood)
        {
            this.Mood = mood;
        }

        /// <summary>
        /// Gets the mood.
        /// </summary>
        public string Mood { get; }
    }
}
