// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActorResolverService.cs" company="MLambda">
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

namespace MLambda.Actors.Satellite.Resolver
{
    using System;
    using System.Collections.Concurrent;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Satellite.Abstraction;

    /// <summary>
    /// Thread-safe service that resolves route names to actor addresses,
    /// creating actors lazily on demand.
    /// </summary>
    public class ActorResolverService : IActorResolver
    {
        private readonly ConcurrentDictionary<string, IAddress> localActors;
        private readonly ActorTypeRegistry typeRegistry;
        private readonly IUserContext userContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorResolverService"/> class.
        /// </summary>
        /// <param name="typeRegistry">The actor type registry.</param>
        /// <param name="userContext">The user context for spawning actors.</param>
        public ActorResolverService(
            ActorTypeRegistry typeRegistry,
            IUserContext userContext)
        {
            this.localActors = new ConcurrentDictionary<string, IAddress>(StringComparer.OrdinalIgnoreCase);
            this.typeRegistry = typeRegistry;
            this.userContext = userContext;
        }

        /// <inheritdoc/>
        public IAddress ResolveLocal(string route)
        {
            return this.localActors.TryGetValue(route, out var address) ? address : null;
        }

        /// <inheritdoc/>
        public IAddress Resolve(string route)
        {
            if (this.localActors.TryGetValue(route, out var existing))
            {
                return existing;
            }

            if (!this.typeRegistry.TryGetType(route, out var actorType))
            {
                return null;
            }

            var address = this.localActors.GetOrAdd(route, _ =>
            {
                var newAddress = this.userContext.Spawn(actorType).Wait();
                return newAddress;
            });

            return address;
        }

        /// <inheritdoc/>
        public void Invalidate(string route)
        {
            this.localActors.TryRemove(route, out _);
        }

        /// <inheritdoc/>
        public void Register(string route, IAddress address)
        {
            this.localActors[route] = address;
        }
    }
}
