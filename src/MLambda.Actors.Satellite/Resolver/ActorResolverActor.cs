// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActorResolverActor.cs" company="MLambda">
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
    using System.Reactive;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Satellite.Abstraction;

    /// <summary>
    /// System actor that handles resolver lifecycle events.
    /// Coordinates local resolver invalidation when routes are removed.
    /// Failover is handled by the cluster-side RouteActor.
    /// </summary>
    [Route("resolver")]
    public class ActorResolverActor : Actor
    {
        private readonly IActorResolver resolver;
        private readonly ActorTypeRegistry typeRegistry;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorResolverActor"/> class.
        /// </summary>
        /// <param name="resolver">The actor resolver service.</param>
        /// <param name="typeRegistry">The actor type registry.</param>
        public ActorResolverActor(
            IActorResolver resolver,
            ActorTypeRegistry typeRegistry)
        {
            this.resolver = resolver;
            this.typeRegistry = typeRegistry;
        }

        /// <inheritdoc/>
        protected override Behavior Receive(object data)
            => data switch
            {
                ResolverNodeLeft msg => Actor.Behavior<Unit, ResolverNodeLeft>(this.HandleNodeLeft, msg),
                _ => Actor.Ignore,
            };

        private IObservable<Unit> HandleNodeLeft(ResolverNodeLeft msg)
        {
            // Invalidate local cached actors from the departed node.
            foreach (var route in this.typeRegistry.GetAllRoutes())
            {
                this.resolver.Invalidate(route);
            }

            return Actor.Done;
        }
    }
}
