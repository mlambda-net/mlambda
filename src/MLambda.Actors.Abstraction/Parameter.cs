// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Parameter.cs" company="MLambda">
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
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;

    /// <summary>
    /// A dynamic parameter object that maps property names to values
    /// using lowercase keys. Used for parameterized route resolution.
    /// </summary>
    /// <example>
    /// <code>
    /// // Indexer syntax:
    /// var p = new Parameter { ["id"] = 112233 };
    ///
    /// // Dynamic syntax:
    /// dynamic p = new Parameter();
    /// p.id = 112233;
    /// </code>
    /// </example>
    public class Parameter : DynamicObject
    {
        private readonly Dictionary<string, object> values;

        /// <summary>
        /// Initializes a new instance of the <see cref="Parameter"/> class.
        /// </summary>
        public Parameter()
        {
            this.values = new Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Parameter"/> class
        /// from an existing dictionary. All keys are lowercased.
        /// </summary>
        /// <param name="values">The initial parameter values.</param>
        public Parameter(Dictionary<string, object> values)
        {
            this.values = new Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase);
            if (values != null)
            {
                foreach (var kvp in values)
                {
                    this.values[kvp.Key.ToLowerInvariant()] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this parameter set is empty.
        /// </summary>
        public bool IsEmpty => this.values.Count == 0;

        /// <summary>
        /// Gets or sets the parameter value by key. Keys are lowercased.
        /// </summary>
        /// <param name="key">The parameter name.</param>
        /// <returns>The parameter value, or null if not found.</returns>
        public object this[string key]
        {
            get => this.values.TryGetValue(key.ToLowerInvariant(), out var val) ? val : null;
            set => this.values[key.ToLowerInvariant()] = value;
        }

        /// <inheritdoc/>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            this.values[binder.Name.ToLowerInvariant()] = value;
            return true;
        }

        /// <inheritdoc/>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return this.values.TryGetValue(binder.Name.ToLowerInvariant(), out result);
        }

        /// <summary>
        /// Converts this parameter set to a dictionary.
        /// </summary>
        /// <returns>A new dictionary with lowercase keys.</returns>
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>(this.values, System.StringComparer.OrdinalIgnoreCase);
        }
    }
}
