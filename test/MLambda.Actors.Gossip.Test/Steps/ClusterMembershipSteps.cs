// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClusterMembershipSteps.cs" company="MLambda">
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

namespace MLambda.Actors.Gossip.Test.Steps
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using System.Threading;
    using MLambda.Actors.Gossip.Abstraction;
    using MLambda.Actors.Network;
    using MLambda.Actors.Network.Abstraction;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for cluster membership tests.
    /// </summary>
    [Binding]
    public class ClusterMembershipSteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterMembershipSteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public ClusterMembershipSteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates a cluster node on a random port.
        /// </summary>
        /// <param name="name">The node name.</param>
        [Given(@"a cluster node (.*) on a random port")]
        public void GivenAClusterNodeOnARandomPort(string name)
        {
            var port = PortAllocator.GetNextPort();
            var nodeId = Guid.NewGuid();
            var endpoint = new NodeEndpoint(nodeId, "127.0.0.1", port);
            var eventStream = new EventStream();
            var serializer = new JsonMessageSerializer();
            var transport = new TcpTransport(endpoint, eventStream);
            var config = new ClusterConfig
            {
                LocalEndpoint = endpoint,
                SeedNodes = new List<NodeEndpoint>(),
                GossipInterval = TimeSpan.FromMilliseconds(200),
                HeartbeatInterval = TimeSpan.FromMilliseconds(100),
                PhiThreshold = 8.0,
                SuspectTimeout = TimeSpan.FromSeconds(5),
            };
            var failureDetector = new PhiAccrualFailureDetector(config.PhiThreshold);
            var manager = new ClusterManager(config, transport, serializer, failureDetector, eventStream);

            this.context[$"endpoint_{name}"] = endpoint;
            this.context[$"transport_{name}"] = transport;
            this.context[$"config_{name}"] = config;
            this.context[$"manager_{name}"] = manager;
        }

        /// <summary>
        /// Creates a cluster node on a random port with a seed node.
        /// </summary>
        /// <param name="name">The node name.</param>
        /// <param name="seedName">The seed node name.</param>
        [Given(@"a cluster node (.*) on a random port with (.*) as seed")]
        public void GivenAClusterNodeOnARandomPortWithSeed(string name, string seedName)
        {
            var port = PortAllocator.GetNextPort();
            var nodeId = Guid.NewGuid();
            var endpoint = new NodeEndpoint(nodeId, "127.0.0.1", port);
            var seedEndpoint = this.context.Get<NodeEndpoint>($"endpoint_{seedName}");
            var eventStream = new EventStream();
            var serializer = new JsonMessageSerializer();
            var transport = new TcpTransport(endpoint, eventStream);
            var config = new ClusterConfig
            {
                LocalEndpoint = endpoint,
                SeedNodes = new List<NodeEndpoint> { seedEndpoint },
                GossipInterval = TimeSpan.FromMilliseconds(200),
                HeartbeatInterval = TimeSpan.FromMilliseconds(100),
                PhiThreshold = 8.0,
                SuspectTimeout = TimeSpan.FromSeconds(5),
            };
            var failureDetector = new PhiAccrualFailureDetector(config.PhiThreshold);
            var manager = new ClusterManager(config, transport, serializer, failureDetector, eventStream);

            this.context[$"endpoint_{name}"] = endpoint;
            this.context[$"transport_{name}"] = transport;
            this.context[$"config_{name}"] = config;
            this.context[$"manager_{name}"] = manager;
        }

        /// <summary>
        /// Starts both cluster nodes.
        /// </summary>
        [Given(@"both cluster nodes are started")]
        [When(@"both cluster nodes are started")]
        public void WhenBothClusterNodesAreStarted()
        {
            var transportA = this.context.Get<TcpTransport>($"transport_A");
            var transportB = this.context.Get<TcpTransport>($"transport_B");

            transportA.Start().Wait();
            transportB.Start().Wait();

            var managerA = this.context.Get<ClusterManager>($"manager_A");
            var managerB = this.context.Get<ClusterManager>($"manager_B");

            managerA.Start();
            managerB.Start();
        }

        /// <summary>
        /// Waits for gossip convergence until both nodes see each other.
        /// </summary>
        [Given(@"we wait for gossip convergence")]
        [When(@"we wait for gossip convergence")]
        public void WhenWeWaitForGossipConvergence()
        {
            var managerA = this.context.Get<ClusterManager>($"manager_A");
            var managerB = this.context.Get<ClusterManager>($"manager_B");

            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < deadline)
            {
                if (managerA.Members.Count >= 2 && managerB.Members.Count >= 2)
                {
                    return;
                }

                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Verifies a node has the expected member count.
        /// </summary>
        /// <param name="name">The node name.</param>
        /// <param name="count">The expected member count.</param>
        [Then(@"node (.*) should have (\d+) members")]
        public void ThenNodeShouldHaveMembers(string name, int count)
        {
            var manager = this.context.Get<ClusterManager>($"manager_{name}");
            manager.Members.Count.ShouldBe(count);
        }

        /// <summary>
        /// Gracefully leaves node B from the cluster.
        /// </summary>
        [When(@"node B gracefully leaves")]
        public void WhenNodeBGracefullyLeaves()
        {
            var manager = this.context.Get<ClusterManager>($"manager_B");
            manager.Leave();
        }

        /// <summary>
        /// Waits for leave propagation.
        /// </summary>
        [When(@"we wait for leave propagation")]
        public void WhenWeWaitForLeavePropagation()
        {
            var managerA = this.context.Get<ClusterManager>($"manager_A");
            var endpointB = this.context.Get<NodeEndpoint>($"endpoint_B");

            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < deadline)
            {
                var member = managerA.GetMember(endpointB.NodeId);
                if (member != null && (member.Status == MemberStatus.Leaving
                    || member.Status == MemberStatus.Down
                    || member.Status == MemberStatus.Removed))
                {
                    return;
                }

                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Verifies node A sees node B as Leaving, Down, or Removed.
        /// </summary>
        [Then(@"node A should see node B as Leaving or Down or Removed")]
        public void ThenNodeAShouldSeeNodeBAsDepartedStatus()
        {
            var managerA = this.context.Get<ClusterManager>($"manager_A");
            var endpointB = this.context.Get<NodeEndpoint>($"endpoint_B");
            var member = managerA.GetMember(endpointB.NodeId);
            member.ShouldNotBeNull();

            var validStatuses = new[]
            {
                MemberStatus.Leaving,
                MemberStatus.Down,
                MemberStatus.Removed,
            };
            validStatuses.ShouldContain(member.Status);
        }

        /// <summary>
        /// Cleans up resources after each scenario.
        /// </summary>
        [AfterScenario]
        public void Cleanup()
        {
            this.TryDispose("manager_A");
            this.TryDispose("manager_B");
            this.TryDispose("transport_A");
            this.TryDispose("transport_B");
        }

        private void TryDispose(string key)
        {
            if (this.context.ContainsKey(key) && this.context[key] is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
