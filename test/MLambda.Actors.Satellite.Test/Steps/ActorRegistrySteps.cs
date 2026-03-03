// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActorRegistrySteps.cs" company="MLambda">
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
    using MLambda.Actors.Network.Abstraction;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for actor registry tests.
    /// </summary>
    [Binding]
    public class ActorRegistrySteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorRegistrySteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public ActorRegistrySteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates a new actor registry.
        /// </summary>
        [Given(@"an actor registry")]
        public void GivenAnActorRegistry()
        {
            this.context["registry"] = new ActorRegistry();
        }

        /// <summary>
        /// Registers an actor on a named node.
        /// </summary>
        /// <param name="actorName">The actor name.</param>
        /// <param name="nodeName">The node name.</param>
        [Given(@"actor (.*) is registered on node (.*)")]
        [When(@"actor (.*) is registered on node (.*)")]
        public void WhenActorIsRegisteredOnNode(string actorName, string nodeName)
        {
            var registry = this.context.Get<ActorRegistry>("registry");
            var actorId = this.GetOrCreateId($"actor_{actorName}");
            var node = this.GetOrCreateNode(nodeName);
            registry.Register(actorId, node);
        }

        /// <summary>
        /// Looks up an actor and verifies the result.
        /// </summary>
        /// <param name="actorName">The actor name.</param>
        /// <param name="nodeName">The expected node name.</param>
        [Then(@"looking up actor (.*) should return node (.*)")]
        public void ThenLookingUpActorShouldReturnNode(string actorName, string nodeName)
        {
            var registry = this.context.Get<ActorRegistry>("registry");
            var actorId = this.context.Get<Guid>($"actor_{actorName}");
            var expectedNode = this.context.Get<NodeEndpoint>($"node_{nodeName}");
            var result = registry.Lookup(actorId);
            result.ShouldNotBeNull();
            result.NodeId.ShouldBe(expectedNode.NodeId);
        }

        /// <summary>
        /// Removes an actor from the registry.
        /// </summary>
        /// <param name="actorName">The actor name.</param>
        [When(@"actor (.*) is removed from the registry")]
        public void WhenActorIsRemovedFromTheRegistry(string actorName)
        {
            var registry = this.context.Get<ActorRegistry>("registry");
            var actorId = this.context.Get<Guid>($"actor_{actorName}");
            registry.Remove(actorId);
        }

        /// <summary>
        /// Verifies that looking up an actor returns null.
        /// </summary>
        /// <param name="actorName">The actor name.</param>
        [Then(@"looking up actor (.*) should return null")]
        public void ThenLookingUpActorShouldReturnNull(string actorName)
        {
            var registry = this.context.Get<ActorRegistry>("registry");
            var actorId = this.context.Get<Guid>($"actor_{actorName}");
            registry.Lookup(actorId).ShouldBeNull();
        }

        /// <summary>
        /// Verifies the registry entry count.
        /// </summary>
        /// <param name="count">The expected count.</param>
        [Then(@"the registry should contain (\d+) entries")]
        public void ThenTheRegistryShouldContainEntries(int count)
        {
            var registry = this.context.Get<ActorRegistry>("registry");
            registry.GetAll().Count.ShouldBe(count);
        }

        private Guid GetOrCreateId(string key)
        {
            if (!this.context.ContainsKey(key))
            {
                this.context[key] = Guid.NewGuid();
            }

            return this.context.Get<Guid>(key);
        }

        private NodeEndpoint GetOrCreateNode(string name)
        {
            var key = $"node_{name}";
            if (!this.context.ContainsKey(key))
            {
                this.context[key] = new NodeEndpoint($"node-{name}", 9000 + name[0]);
            }

            return this.context.Get<NodeEndpoint>(key);
        }
    }
}
