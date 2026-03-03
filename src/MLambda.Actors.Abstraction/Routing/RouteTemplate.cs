// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RouteTemplate.cs" company="MLambda">
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

namespace MLambda.Actors.Abstraction.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Parses, resolves, and matches parameterized route templates.
    /// A template like <c>"manager/{id}"</c> can be resolved with a
    /// <see cref="Parameter"/> to produce <c>"manager/112233"</c>,
    /// and a resolved route can be matched back against the template.
    /// </summary>
    public class RouteTemplate
    {
        private static readonly Regex ParameterPattern = new Regex(
            @"\{(\w+)\}",
            RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteTemplate"/> class.
        /// </summary>
        /// <param name="template">The route template string (e.g. "manager/{id}").</param>
        public RouteTemplate(string template)
        {
            this.Template = template ?? throw new ArgumentNullException(nameof(template));
            this.Segments = template.Split('/');
            this.ParameterNames = ParameterPattern.Matches(template)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value.ToLowerInvariant())
                .ToArray();
            this.IsParameterized = this.ParameterNames.Length > 0;
        }

        /// <summary>
        /// Gets the original template string.
        /// </summary>
        public string Template { get; }

        /// <summary>
        /// Gets the template split into path segments.
        /// </summary>
        public string[] Segments { get; }

        /// <summary>
        /// Gets the lowercase parameter names extracted from the template.
        /// </summary>
        public string[] ParameterNames { get; }

        /// <summary>
        /// Gets a value indicating whether this template contains parameters.
        /// </summary>
        public bool IsParameterized { get; }

        /// <summary>
        /// Resolves the template by substituting parameter values.
        /// </summary>
        /// <param name="parameters">The parameter values.</param>
        /// <returns>The resolved route string (e.g. "manager/112233").</returns>
        public string Resolve(Parameter parameters)
        {
            if (!this.IsParameterized)
            {
                return this.Template;
            }

            if (parameters == null || parameters.IsEmpty)
            {
                throw new ArgumentException(
                    $"Route template '{this.Template}' requires parameters: {string.Join(", ", this.ParameterNames)}");
            }

            var resolved = ParameterPattern.Replace(this.Template, match =>
            {
                var name = match.Groups[1].Value.ToLowerInvariant();
                var value = parameters[name];
                if (value == null)
                {
                    throw new ArgumentException(
                        $"Missing required parameter '{name}' for template '{this.Template}'");
                }

                return value.ToString().ToLowerInvariant();
            });

            return resolved;
        }

        /// <summary>
        /// Checks if a resolved route matches this template and extracts parameters.
        /// </summary>
        /// <param name="resolvedRoute">The resolved route to match against.</param>
        /// <param name="parameters">The extracted parameter values, if matched.</param>
        /// <returns>True if the resolved route matches this template.</returns>
        public bool TryMatch(string resolvedRoute, out Parameter parameters)
        {
            parameters = null;
            if (string.IsNullOrEmpty(resolvedRoute))
            {
                return false;
            }

            var resolvedSegments = resolvedRoute.Split('/');
            if (resolvedSegments.Length != this.Segments.Length)
            {
                return false;
            }

            var extracted = new Parameter();
            for (int i = 0; i < this.Segments.Length; i++)
            {
                var match = ParameterPattern.Match(this.Segments[i]);
                if (match.Success)
                {
                    extracted[match.Groups[1].Value] = resolvedSegments[i];
                }
                else if (!string.Equals(this.Segments[i], resolvedSegments[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            parameters = extracted;
            return true;
        }

        /// <summary>
        /// Gets the base route (segments before the first parameter).
        /// For "manager/{id}" returns "manager". For "greeter" returns "greeter".
        /// </summary>
        /// <returns>The base route string.</returns>
        public string GetBaseRoute()
        {
            var parts = new List<string>();
            foreach (var seg in this.Segments)
            {
                if (ParameterPattern.IsMatch(seg))
                {
                    break;
                }

                parts.Add(seg);
            }

            return string.Join("/", parts);
        }
    }
}
