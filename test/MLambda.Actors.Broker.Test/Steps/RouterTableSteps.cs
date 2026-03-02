// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RouterTableSteps.cs" company="MLambda">
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

namespace MLambda.Actors.Broker.Test.Steps
{
    using System;
    using MLambda.Actors.Broker.Abstraction;
    using MLambda.Actors.Network.Abstraction;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for router table tests.
    /// </summary>
    [Binding]
    public class RouterTableSteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="RouterTableSteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public RouterTableSteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates an empty router table.
        /// </summary>
        [Given(@"an empty router table")]
        public void GivenAnEmptyRouterTable()
        {
            this.context["table"] = new RouterTable();
        }

        /// <summary>
        /// Registers a route on a named node.
        /// </summary>
        /// <param name="route">The actor route.</param>
        /// <param name="nodeName">The node name.</param>
        [Given(@"route ""(.*)"" is registered on node (.*)")]
        [When(@"route ""(.*)"" is registered on node (.*)")]
        public void WhenRouteIsRegisteredOnNode(string route, string nodeName)
        {
            var table = this.context.Get<RouterTable>("table");
            var node = this.GetOrCreateNode(nodeName);
            table.AddRoute(route, node);
        }

        /// <summary>
        /// Removes a route.
        /// </summary>
        /// <param name="route">The actor route.</param>
        [When(@"route ""(.*)"" is removed")]
        public void WhenRouteIsRemoved(string route)
        {
            var table = this.context.Get<RouterTable>("table");
            table.RemoveRoute(route);
        }

        /// <summary>
        /// Removes all routes for a named node.
        /// </summary>
        /// <param name="nodeName">The node name.</param>
        [When(@"all routes for node (.*) are removed")]
        public void WhenAllRoutesForNodeAreRemoved(string nodeName)
        {
            var table = this.context.Get<RouterTable>("table");
            var node = this.context.Get<NodeEndpoint>($"node_{nodeName}");
            table.RemoveNode(node.NodeId);
        }

        /// <summary>
        /// Verifies a route lookup returns the expected node.
        /// </summary>
        /// <param name="route">The actor route.</param>
        /// <param name="nodeName">The expected node name.</param>
        [Then(@"looking up route ""(.*)"" should return node (.*)")]
        public void ThenLookingUpRouteShouldReturnNode(string route, string nodeName)
        {
            var table = this.context.Get<RouterTable>("table");
            var expectedNode = this.context.Get<NodeEndpoint>($"node_{nodeName}");
            var result = table.LookupRoute(route);
            result.ShouldNotBeNull();
            result.NodeId.ShouldBe(expectedNode.NodeId);
        }

        /// <summary>
        /// Verifies a route lookup returns null.
        /// </summary>
        /// <param name="route">The actor route.</param>
        [Then(@"looking up route ""(.*)"" should return null")]
        public void ThenLookingUpRouteShouldReturnNull(string route)
        {
            var table = this.context.Get<RouterTable>("table");
            table.LookupRoute(route).ShouldBeNull();
        }

        /// <summary>
        /// Verifies the router table entry count.
        /// </summary>
        /// <param name="count">The expected count.</param>
        [Then(@"the router table should contain (\d+) entries")]
        public void ThenTheRouterTableShouldContainEntries(int count)
        {
            var table = this.context.Get<RouterTable>("table");
            table.GetAll().Count.ShouldBe(count);
        }

        private NodeEndpoint GetOrCreateNode(string name)
        {
            var key = $"node_{name}";
            if (!this.context.ContainsKey(key))
            {
                this.context[key] = new NodeEndpoint(Guid.NewGuid(), "127.0.0.1", 9000 + name[0]);
            }

            return this.context.Get<NodeEndpoint>(key);
        }
    }
}
