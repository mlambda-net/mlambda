// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AsteroidLifecycleSteps.cs" company="MLambda">
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

namespace MLambda.Actors.Asteroids.Test.Steps
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using MLambda.Actors.Asteroids.Lifecycle;
    using MLambda.Actors.Cluster.Abstraction;
    using MLambda.Actors.Network;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Satellite.Abstraction;
    using Moq;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for asteroid lifecycle tests.
    /// </summary>
    [Binding]
    public class AsteroidLifecycleSteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsteroidLifecycleSteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public AsteroidLifecycleSteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates an asteroid service with no cluster nodes configured.
        /// </summary>
        [Given(@"an asteroid service with no cluster nodes configured")]
        public void GivenAnAsteroidServiceWithNoClusterNodes()
        {
            var config = new ActorCatalogConfig
            {
                NodeId = "asteroid-test",
                Port = 9000,
                ClusterNodes = new List<NodeEndpoint>(),
            };

            var mockTransport = new Mock<ITransport>();
            var serializer = new JsonMessageSerializer();

            var service = new AsteroidService(config, mockTransport.Object, serializer);
            this.context["service"] = service;
        }

        /// <summary>
        /// Creates an asteroid service with cluster nodes configured.
        /// </summary>
        /// <param name="count">The number of cluster nodes.</param>
        [Given(@"an asteroid service with (\d+) cluster nodes configured")]
        public void GivenAnAsteroidServiceWithClusterNodes(int count)
        {
            var clusterNodes = new List<NodeEndpoint>();
            for (int i = 0; i < count; i++)
            {
                clusterNodes.Add(new NodeEndpoint($"cluster-{i}", 9100 + i));
            }

            var config = new ActorCatalogConfig
            {
                NodeId = "asteroid-test",
                Port = 9000,
                ClusterNodes = clusterNodes,
            };

            var sentEnvelopes = new ConcurrentBag<(NodeEndpoint Target, Envelope Envelope)>();
            var mockTransport = new Mock<ITransport>();
            mockTransport.Setup(t => t.Send(It.IsAny<NodeEndpoint>(), It.IsAny<Envelope>()))
                .Callback<NodeEndpoint, Envelope>((node, env) => sentEnvelopes.Add((node, env)))
                .Returns(Observable.Return(Unit.Default));

            var serializer = new JsonMessageSerializer();

            var service = new AsteroidService(config, mockTransport.Object, serializer);
            this.context["service"] = service;
            this.context["sentEnvelopes"] = sentEnvelopes;
            this.context["serializer"] = serializer;
            this.context["clusterNodeCount"] = count;
        }

        /// <summary>
        /// Attempts to start the asteroid service and captures the exception.
        /// </summary>
        [When(@"the asteroid service is started")]
        public void WhenTheAsteroidServiceIsStarted()
        {
            var service = this.context.Get<AsteroidService>("service");
            try
            {
                service.Start();
            }
            catch (Exception ex)
            {
                this.context["exception"] = ex;
            }
        }

        /// <summary>
        /// Starts the asteroid service successfully.
        /// </summary>
        [Given(@"the asteroid service is started successfully")]
        [When(@"the asteroid service is started successfully")]
        public void WhenTheAsteroidServiceIsStartedSuccessfully()
        {
            var service = this.context.Get<AsteroidService>("service");
            service.Start();
        }

        /// <summary>
        /// Stops the asteroid service.
        /// </summary>
        [When(@"the asteroid service is stopped")]
        public void WhenTheAsteroidServiceIsStopped()
        {
            var service = this.context.Get<AsteroidService>("service");

            // Clear previously captured envelopes so we only see disconnect messages.
            if (this.context.ContainsKey("sentEnvelopes"))
            {
                var old = this.context.Get<ConcurrentBag<(NodeEndpoint Target, Envelope Envelope)>>("sentEnvelopes");
                while (old.TryTake(out _))
                {
                }
            }

            service.Stop();
        }

        /// <summary>
        /// Verifies an InvalidOperationException was thrown.
        /// </summary>
        [Then(@"an InvalidOperationException should be thrown")]
        public void ThenAnInvalidOperationExceptionShouldBeThrown()
        {
            this.context.ContainsKey("exception").ShouldBeTrue();
            this.context.Get<Exception>("exception").ShouldBeOfType<InvalidOperationException>();
        }

        /// <summary>
        /// Verifies AsteroidRegister envelopes were sent to each cluster node.
        /// </summary>
        [Then(@"an AsteroidRegister envelope should be sent to each cluster node")]
        public void ThenRegisterEnvelopesShouldBeSent()
        {
            var sentEnvelopes = this.context.Get<ConcurrentBag<(NodeEndpoint Target, Envelope Envelope)>>("sentEnvelopes");
            var count = this.context.Get<int>("clusterNodeCount");
            var serializer = this.context.Get<JsonMessageSerializer>("serializer");

            var registerEnvelopes = sentEnvelopes
                .Where(e => e.Envelope.Type == EnvelopeType.Topology)
                .ToList();

            registerEnvelopes.Count.ShouldBe(count);

            foreach (var (_, env) in registerEnvelopes)
            {
                env.TargetRoute.ShouldBe("route");
                var payload = serializer.Deserialize(env.PayloadBytes, env.PayloadTypeName);
                payload.ShouldBeOfType<AsteroidRegister>();
            }
        }

        /// <summary>
        /// Verifies AsteroidDisconnected envelopes were sent to each cluster node.
        /// </summary>
        [Then(@"an AsteroidDisconnected envelope should be sent to each cluster node")]
        public void ThenDisconnectEnvelopesShouldBeSent()
        {
            var sentEnvelopes = this.context.Get<ConcurrentBag<(NodeEndpoint Target, Envelope Envelope)>>("sentEnvelopes");
            var count = this.context.Get<int>("clusterNodeCount");
            var serializer = this.context.Get<JsonMessageSerializer>("serializer");

            var disconnectEnvelopes = sentEnvelopes
                .Where(e => e.Envelope.Type == EnvelopeType.Topology)
                .ToList();

            disconnectEnvelopes.Count.ShouldBe(count);

            foreach (var (_, env) in disconnectEnvelopes)
            {
                var payload = serializer.Deserialize(env.PayloadBytes, env.PayloadTypeName);
                payload.ShouldBeOfType<AsteroidDisconnected>();
            }
        }
    }
}
