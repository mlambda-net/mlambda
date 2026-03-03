// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TypeRegistrySteps.cs" company="MLambda">
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

namespace MLambda.Actors.Satellite.Test.Steps
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Abstraction.Supervision;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for actor type registry tests.
    /// </summary>
    [Binding]
    public class TypeRegistrySteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeRegistrySteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public TypeRegistrySteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates a registry with a simple route actor.
        /// </summary>
        [Given(@"an actor type registry with a simple route actor")]
        public void GivenRegistryWithSimpleActor()
        {
            var registry = new ActorTypeRegistry(new List<Type> { typeof(TestGreeterActor) });
            this.context["registry"] = registry;
        }

        /// <summary>
        /// Creates a registry with a parameterized route actor.
        /// </summary>
        [Given(@"an actor type registry with a parameterized route actor")]
        public void GivenRegistryWithParameterizedActor()
        {
            var registry = new ActorTypeRegistry(new List<Type> { typeof(TestManagerActor) });
            this.context["registry"] = registry;
        }

        /// <summary>
        /// Creates a registry with both simple and parameterized route actors.
        /// </summary>
        [Given(@"an actor type registry with both simple and parameterized actors")]
        public void GivenRegistryWithBothActors()
        {
            var registry = new ActorTypeRegistry(new List<Type>
            {
                typeof(TestGreeterActor),
                typeof(TestManagerActor),
            });
            this.context["registry"] = registry;
        }

        /// <summary>
        /// Looks up a route in the registry.
        /// </summary>
        /// <param name="route">The route to look up.</param>
        [When(@"looking up route ""(.*)""")]
        public void WhenLookingUpRoute(string route)
        {
            var registry = this.context.Get<ActorTypeRegistry>("registry");
            var found = registry.TryGetType(route, out var actorType);
            this.context["lookupResult"] = found;
            if (found)
            {
                this.context["resolvedType"] = actorType;
            }
        }

        /// <summary>
        /// Gets all routes from the registry.
        /// </summary>
        [When(@"getting all routes")]
        public void WhenGettingAllRoutes()
        {
            var registry = this.context.Get<ActorTypeRegistry>("registry");
            this.context["allRoutes"] = registry.GetAllRoutes().ToList();
        }

        /// <summary>
        /// Verifies the resolved type is the simple route actor.
        /// </summary>
        [Then(@"the resolved type should be the simple route actor type")]
        public void ThenResolvedTypeShouldBeSimple()
        {
            this.context.Get<bool>("lookupResult").ShouldBeTrue();
            this.context.Get<Type>("resolvedType").ShouldBe(typeof(TestGreeterActor));
        }

        /// <summary>
        /// Verifies the resolved type is the parameterized route actor.
        /// </summary>
        [Then(@"the resolved type should be the parameterized route actor type")]
        public void ThenResolvedTypeShouldBeParameterized()
        {
            this.context.Get<bool>("lookupResult").ShouldBeTrue();
            this.context.Get<Type>("resolvedType").ShouldBe(typeof(TestManagerActor));
        }

        /// <summary>
        /// Verifies the type lookup failed.
        /// </summary>
        [Then(@"the type lookup should fail")]
        public void ThenTypeLookupShouldFail()
        {
            this.context.Get<bool>("lookupResult").ShouldBeFalse();
        }

        /// <summary>
        /// Verifies all routes include both expected routes.
        /// </summary>
        /// <param name="route1">The first expected route.</param>
        /// <param name="route2">The second expected route.</param>
        [Then(@"the routes should include ""(.*)"" and ""(.*)""")]
        public void ThenRoutesShouldInclude(string route1, string route2)
        {
            var routes = this.context.Get<List<string>>("allRoutes");
            routes.ShouldContain(route1);
            routes.ShouldContain(route2);
        }

        /// <summary>
        /// A test actor with a simple route.
        /// </summary>
        [Route("greeter")]
        private class TestGreeterActor : Actor
        {
            /// <inheritdoc/>
            protected override Behavior Receive(object data) => Ignore;
        }

        /// <summary>
        /// A test actor with a parameterized route.
        /// </summary>
        [Route("manager/{id}")]
        private class TestManagerActor : Actor
        {
            /// <inheritdoc/>
            protected override Behavior Receive(object data) => Ignore;
        }
    }
}
