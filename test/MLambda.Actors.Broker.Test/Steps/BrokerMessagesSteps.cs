// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BrokerMessagesSteps.cs" company="MLambda">
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
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Broker.Abstraction;
    using MLambda.Actors.Network.Abstraction;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for broker message handling tests.
    /// </summary>
    [Binding]
    public class BrokerMessagesSteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokerMessagesSteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public BrokerMessagesSteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates a broker actor with an empty router table.
        /// </summary>
        [Given(@"a broker actor with an empty router table")]
        public void GivenABrokerActorWithAnEmptyRouterTable()
        {
            var table = new RouterTable();
            var actor = new BrokerActor(table);
            this.context["broker"] = actor;
            this.context["table"] = table;
        }

        /// <summary>
        /// Creates a broker actor with a pre-registered route.
        /// </summary>
        /// <param name="route">The route to register.</param>
        /// <param name="nodeName">The node name.</param>
        [Given(@"a broker actor with route ""(.*)"" on node (.*)")]
        public void GivenABrokerActorWithRouteOnNode(string route, string nodeName)
        {
            var table = new RouterTable();
            var node = this.GetOrCreateNode(nodeName);
            table.AddRoute(route, node);
            var actor = new BrokerActor(table);
            this.context["broker"] = actor;
            this.context["table"] = table;
        }

        /// <summary>
        /// Sends a RegisterRoute message.
        /// </summary>
        /// <param name="route">The route to register.</param>
        [When(@"a RegisterRoute message for ""(.*)"" is sent")]
        public void WhenARegisterRouteMessageIsSent(string route)
        {
            var actor = this.context.Get<BrokerActor>("broker");
            var behavior = ((IActor)actor).Receive(new RegisterRoute { Route = route });
            behavior(null).Wait();
        }

        /// <summary>
        /// Sends a LookupRoute message.
        /// </summary>
        /// <param name="route">The route to look up.</param>
        [When(@"a LookupRoute message for ""(.*)"" is sent")]
        public void WhenALookupRouteMessageIsSent(string route)
        {
            var actor = this.context.Get<BrokerActor>("broker");
            var behavior = ((IActor)actor).Receive(new LookupRoute { Route = route });
            var result = behavior(null).Wait();
            this.context["lookupResult"] = result;
        }

        /// <summary>
        /// Sends an AnnounceRoutes message.
        /// </summary>
        /// <param name="nodeName">The source node name.</param>
        /// <param name="route1">The first route.</param>
        /// <param name="route2">The second route.</param>
        [When(@"an AnnounceRoutes message arrives from node (.*) with routes ""(.*)"" and ""(.*)""")]
        public void WhenAnAnnounceRoutesMessageArrives(string nodeName, string route1, string route2)
        {
            var actor = this.context.Get<BrokerActor>("broker");
            var node = this.GetOrCreateNode(nodeName);
            var msg = new AnnounceRoutes
            {
                SourceNode = node,
                Routes = new List<string> { route1, route2 },
            };
            var behavior = ((IActor)actor).Receive(msg);
            behavior(null).Wait();
        }

        /// <summary>
        /// Sends a NodeLeft message.
        /// </summary>
        /// <param name="nodeName">The node name.</param>
        [When(@"a NodeLeft message for node (.*) is sent")]
        public void WhenANodeLeftMessageIsSent(string nodeName)
        {
            var actor = this.context.Get<BrokerActor>("broker");
            var node = this.context.Get<NodeEndpoint>($"node_{nodeName}");
            var behavior = ((IActor)actor).Receive(new NodeLeft { NodeId = node.NodeId });
            behavior(null).Wait();
        }

        /// <summary>
        /// Verifies the router table contains a route.
        /// </summary>
        /// <param name="route">The expected route.</param>
        [Then(@"the router table should contain route ""(.*)""")]
        public void ThenTheRouterTableShouldContainRoute(string route)
        {
            var table = this.context.Get<RouterTable>("table");
            table.GetAll().ContainsKey(route).ShouldBeTrue();
        }

        /// <summary>
        /// Verifies the lookup result contains the route.
        /// </summary>
        /// <param name="route">The expected route.</param>
        [Then(@"the lookup result should contain route ""(.*)""")]
        public void ThenTheLookupResultShouldContainRoute(string route)
        {
            var result = this.context["lookupResult"];
            result.ShouldBeOfType<LookupRouteResult>();
            ((LookupRouteResult)result).Route.ShouldBe(route);
        }

        /// <summary>
        /// Verifies a route lookup in the table returns null.
        /// </summary>
        /// <param name="route">The route to look up.</param>
        [Then(@"looking up route ""(.*)"" in the table should return null")]
        public void ThenLookingUpRouteInTheTableShouldReturnNull(string route)
        {
            var table = this.context.Get<RouterTable>("table");
            table.LookupRoute(route).ShouldBeNull();
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
