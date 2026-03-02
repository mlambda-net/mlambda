// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DiscoveryTrackingSteps.cs" company="MLambda">
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
    using MLambda.Actors.Broker.Abstraction;
    using MLambda.Actors.Network.Abstraction;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for discovery tracking tests.
    /// </summary>
    [Binding]
    public class DiscoveryTrackingSteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryTrackingSteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public DiscoveryTrackingSteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates a discovery actor.
        /// </summary>
        [Given(@"a discovery actor")]
        public void GivenADiscoveryActor()
        {
            this.context["discovery"] = new DiscoveryActor();
        }

        /// <summary>
        /// Sends an AnnounceRoutes message to the discovery actor.
        /// </summary>
        /// <param name="nodeName">The source node name.</param>
        /// <param name="route1">The first route.</param>
        /// <param name="route2">The second route.</param>
        [Given(@"the discovery actor receives an AnnounceRoutes from node (.*) with routes ""(.*)"" and ""(.*)""")]
        [When(@"the discovery actor receives an AnnounceRoutes from node (.*) with routes ""(.*)"" and ""(.*)""")]
        public void WhenTheDiscoveryActorReceivesAnnounceRoutes(string nodeName, string route1, string route2)
        {
            var actor = this.context.Get<DiscoveryActor>("discovery");
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
        /// Sends a NodeLeft message to the discovery actor.
        /// </summary>
        /// <param name="nodeName">The node name.</param>
        [When(@"a NodeLeft message for node (.*) is sent to discovery")]
        public void WhenANodeLeftMessageIsSentToDiscovery(string nodeName)
        {
            var actor = this.context.Get<DiscoveryActor>("discovery");
            var node = this.context.Get<NodeEndpoint>($"node_{nodeName}");
            var behavior = ((IActor)actor).Receive(new NodeLeft { NodeId = node.NodeId });
            behavior(null).Wait();
        }

        /// <summary>
        /// Verifies the discovery routes count.
        /// </summary>
        /// <param name="count">The expected count.</param>
        [Then(@"discovering routes should return (\d+) entries")]
        public void ThenDiscoveringRoutesShouldReturnEntries(int count)
        {
            var actor = this.context.Get<DiscoveryActor>("discovery");
            var behavior = ((IActor)actor).Receive(new DiscoverRoutes());
            var result = (DiscoverRoutesResult)behavior(null).Wait();
            result.Routes.Count.ShouldBe(count);
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
