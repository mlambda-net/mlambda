// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActorTypeRegistry.cs" company="MLambda">
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

namespace MLambda.Actors.Satellite
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Abstraction.Routing;

    /// <summary>
    /// Maps route names (from <see cref="RouteAttribute"/>) to their CLR actor types.
    /// Supports both simple routes (exact match) and parameterized templates
    /// (e.g. "manager/{id}" matching "manager/112233").
    /// Built from the actor types registered via <c>config.Register&lt;T&gt;()</c>.
    /// </summary>
    public class ActorTypeRegistry
    {
        private readonly Dictionary<string, Type> exactRoutes;
        private readonly List<(RouteTemplate Template, Type ActorType)> parameterizedRoutes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorTypeRegistry"/> class.
        /// </summary>
        /// <param name="actorTypes">The registered actor types.</param>
        public ActorTypeRegistry(IReadOnlyList<Type> actorTypes)
        {
            this.exactRoutes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            this.parameterizedRoutes = new List<(RouteTemplate, Type)>();

            foreach (var type in actorTypes)
            {
                var route = type.GetCustomAttribute<RouteAttribute>();
                var name = route?.Name ?? type.Name;
                var template = new RouteTemplate(name);

                if (template.IsParameterized)
                {
                    this.parameterizedRoutes.Add((template, type));
                }
                else
                {
                    this.exactRoutes[name] = type;
                }
            }
        }

        /// <summary>
        /// Tries to get the CLR type for a given route name.
        /// First attempts exact match for simple routes, then
        /// falls back to template matching for parameterized routes.
        /// </summary>
        /// <param name="route">The route name (simple or resolved).</param>
        /// <param name="actorType">The resolved actor type.</param>
        /// <returns>True if the route was found; otherwise false.</returns>
        public bool TryGetType(string route, out Type actorType)
        {
            // Fast path: exact match for simple routes.
            if (this.exactRoutes.TryGetValue(route, out actorType))
            {
                return true;
            }

            // Slow path: template matching for parameterized routes.
            foreach (var (template, type) in this.parameterizedRoutes)
            {
                if (template.TryMatch(route, out _))
                {
                    actorType = type;
                    return true;
                }
            }

            actorType = null;
            return false;
        }

        /// <summary>
        /// Gets all registered route names and templates.
        /// </summary>
        /// <returns>The collection of route names and template strings.</returns>
        public IEnumerable<string> GetAllRoutes()
        {
            return this.exactRoutes.Keys
                .Concat(this.parameterizedRoutes.Select(p => p.Template.Template));
        }
    }
}
