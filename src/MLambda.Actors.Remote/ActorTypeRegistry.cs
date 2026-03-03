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

namespace MLambda.Actors.Remote
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using MLambda.Actors.Abstraction.Annotation;

    /// <summary>
    /// Maps route names (from <see cref="RouteAttribute"/>) to their CLR actor types.
    /// Built from the actor types registered via <c>config.Register&lt;T&gt;()</c>.
    /// </summary>
    public class ActorTypeRegistry
    {
        private readonly Dictionary<string, Type> routeToType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorTypeRegistry"/> class.
        /// </summary>
        /// <param name="actorTypes">The registered actor types.</param>
        public ActorTypeRegistry(IReadOnlyList<Type> actorTypes)
        {
            this.routeToType = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            foreach (var type in actorTypes)
            {
                var route = type.GetCustomAttribute<RouteAttribute>();
                var name = route?.Name ?? type.Name;
                this.routeToType[name] = type;
            }
        }

        /// <summary>
        /// Tries to get the CLR type for a given route name.
        /// </summary>
        /// <param name="route">The route name.</param>
        /// <param name="actorType">The resolved actor type.</param>
        /// <returns>True if the route was found; otherwise false.</returns>
        public bool TryGetType(string route, out Type actorType)
        {
            return this.routeToType.TryGetValue(route, out actorType);
        }

        /// <summary>
        /// Gets all registered route names.
        /// </summary>
        /// <returns>The collection of route names.</returns>
        public IEnumerable<string> GetAllRoutes()
        {
            return this.routeToType.Keys;
        }
    }
}
