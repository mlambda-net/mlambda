// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AsteroidDispatcherSteps.cs" company="MLambda">
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
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading.Tasks;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Satellite.Abstraction;
    using Moq;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for asteroid message dispatcher tests.
    /// </summary>
    [Binding]
    public class AsteroidDispatcherSteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsteroidDispatcherSteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public AsteroidDispatcherSteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates an asteroid message dispatcher with a pending request.
        /// </summary>
        [Given(@"an asteroid message dispatcher with a pending request")]
        public void GivenADispatcherWithPendingRequest()
        {
            var correlationId = Guid.NewGuid();
            var tcs = new TaskCompletionSource<object>();
            var pendingRequests = new ConcurrentDictionary<Guid, TaskCompletionSource<object>>();
            pendingRequests[correlationId] = tcs;

            var subject = new Subject<Envelope>();
            var mockSerializer = new Mock<IMessageSerializer>();
            mockSerializer.Setup(s => s.Deserialize(It.IsAny<byte[]>(), It.IsAny<string>()))
                .Returns("test_payload");

            var mockTransport = new Mock<ITransport>();
            mockTransport.Setup(t => t.IncomingMessages).Returns(subject);

            var mockResolver = new Mock<IActorResolver>();

            var dispatcher = new AsteroidMessageDispatcher(
                mockTransport.Object, mockSerializer.Object, mockResolver.Object, pendingRequests);
            dispatcher.Start();

            this.context["dispatcher"] = dispatcher;
            this.context["subject"] = subject;
            this.context["correlationId"] = correlationId;
            this.context["tcs"] = tcs;
            this.context["pendingRequests"] = pendingRequests;
        }

        /// <summary>
        /// Creates an asteroid message dispatcher with no pending requests.
        /// </summary>
        [Given(@"an asteroid message dispatcher with no pending requests")]
        public void GivenADispatcherWithNoPendingRequests()
        {
            var pendingRequests = new ConcurrentDictionary<Guid, TaskCompletionSource<object>>();
            var subject = new Subject<Envelope>();

            var mockSerializer = new Mock<IMessageSerializer>();
            mockSerializer.Setup(s => s.Deserialize(It.IsAny<byte[]>(), It.IsAny<string>()))
                .Returns("test_payload");

            var mockTransport = new Mock<ITransport>();
            mockTransport.Setup(t => t.IncomingMessages).Returns(subject);

            var mockResolver = new Mock<IActorResolver>();

            var dispatcher = new AsteroidMessageDispatcher(
                mockTransport.Object, mockSerializer.Object, mockResolver.Object, pendingRequests);
            dispatcher.Start();

            this.context["dispatcher"] = dispatcher;
            this.context["subject"] = subject;
        }

        /// <summary>
        /// Creates an asteroid message dispatcher with a topology handler.
        /// </summary>
        [Given(@"an asteroid message dispatcher with a topology handler")]
        public void GivenADispatcherWithTopologyHandler()
        {
            var pendingRequests = new ConcurrentDictionary<Guid, TaskCompletionSource<object>>();
            var subject = new Subject<Envelope>();
            var topologyReceived = false;

            var mockSerializer = new Mock<IMessageSerializer>();
            mockSerializer.Setup(s => s.Deserialize(It.IsAny<byte[]>(), It.IsAny<string>()))
                .Returns("topology_payload");

            var mockAddress = new Mock<IAddress>();
            mockAddress.Setup(a => a.Send(It.IsAny<object>()))
                .Callback<object>(_ => topologyReceived = true)
                .Returns(Observable.Return(Unit.Default));

            var mockResolver = new Mock<IActorResolver>();
            mockResolver.Setup(r => r.Resolve("dispatcher"))
                .Returns(mockAddress.Object);

            var mockTransport = new Mock<ITransport>();
            mockTransport.Setup(t => t.IncomingMessages).Returns(subject);

            var dispatcher = new AsteroidMessageDispatcher(
                mockTransport.Object, mockSerializer.Object, mockResolver.Object, pendingRequests);
            dispatcher.Start();

            this.context["dispatcher"] = dispatcher;
            this.context["subject"] = subject;
            this.context["topologyReceived"] = topologyReceived;
            this.context["mockAddress"] = mockAddress;
        }

        /// <summary>
        /// Sends a response envelope with the matching correlation id.
        /// </summary>
        [When(@"a response envelope arrives with the matching correlation id")]
        public void WhenAResponseEnvelopeArrivesWithMatchingId()
        {
            var subject = this.context.Get<Subject<Envelope>>("subject");
            var correlationId = this.context.Get<Guid>("correlationId");

            subject.OnNext(new Envelope
            {
                CorrelationId = correlationId,
                Type = EnvelopeType.Response,
                PayloadTypeName = "System.String",
                PayloadBytes = new byte[] { 0 },
            });
        }

        /// <summary>
        /// Sends a response envelope with an unknown correlation id.
        /// </summary>
        [When(@"a response envelope arrives with an unknown correlation id")]
        public void WhenAResponseEnvelopeArrivesWithUnknownId()
        {
            var subject = this.context.Get<Subject<Envelope>>("subject");

            subject.OnNext(new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                Type = EnvelopeType.Response,
                PayloadTypeName = "System.String",
                PayloadBytes = new byte[] { 0 },
            });
        }

        /// <summary>
        /// Sends a topology envelope with a target route.
        /// </summary>
        /// <param name="targetRoute">The target route.</param>
        [When(@"a topology envelope arrives with target route ""(.*)""")]
        public void WhenATopologyEnvelopeArrives(string targetRoute)
        {
            var subject = this.context.Get<Subject<Envelope>>("subject");

            subject.OnNext(new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetRoute = targetRoute,
                Type = EnvelopeType.Topology,
                PayloadTypeName = "System.String",
                PayloadBytes = new byte[] { 0 },
            });
        }

        /// <summary>
        /// Verifies the pending request was completed with the response payload.
        /// </summary>
        [Then(@"the pending request should be completed with the response payload")]
        public void ThenThePendingRequestShouldBeCompleted()
        {
            var tcs = this.context.Get<TaskCompletionSource<object>>("tcs");
            tcs.Task.Wait(TimeSpan.FromSeconds(2));
            tcs.Task.IsCompleted.ShouldBeTrue();
            tcs.Task.Result.ShouldBe("test_payload");
        }

        /// <summary>
        /// Verifies no exception was thrown.
        /// </summary>
        [Then(@"no exception should be thrown")]
        public void ThenNoExceptionShouldBeThrown()
        {
            // If we got here, no exception was thrown during envelope processing.
            true.ShouldBeTrue();
        }

        /// <summary>
        /// Verifies topology payload was routed to local actor.
        /// </summary>
        [Then(@"the topology payload should be routed to the local actor")]
        public void ThenTheTopologyPayloadShouldBeRouted()
        {
            var mockAddress = this.context.Get<Mock<IAddress>>("mockAddress");
            mockAddress.Verify(a => a.Send(It.IsAny<object>()), Times.Once);
        }
    }
}
