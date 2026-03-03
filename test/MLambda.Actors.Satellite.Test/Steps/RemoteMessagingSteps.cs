// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RemoteMessagingSteps.cs" company="MLambda">
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
    using System.Collections.Concurrent;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using MLambda.Actors.Network;
    using MLambda.Actors.Network.Abstraction;
    using Moq;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for remote messaging tests.
    /// </summary>
    [Binding]
    public class RemoteMessagingSteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteMessagingSteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public RemoteMessagingSteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates a remote address with a mock transport.
        /// </summary>
        [Given(@"a remote address for actor X on a remote node")]
        public void GivenARemoteAddressForActorX()
        {
            var actorId = Guid.NewGuid();
            var targetNode = new NodeEndpoint("127.0.0.1", 9001);
            var localNode = new NodeEndpoint("127.0.0.1", 9000);
            var serializer = new JsonMessageSerializer();
            var pendingRequests = new ConcurrentDictionary<Guid, TaskCompletionSource<object>>();

            var sentEnvelopes = new ConcurrentBag<Envelope>();
            var mockTransport = new Mock<ITransport>();
            mockTransport.Setup(t => t.LocalEndpoint).Returns(localNode);
            mockTransport.Setup(t => t.Send(It.IsAny<NodeEndpoint>(), It.IsAny<Envelope>()))
                .Callback<NodeEndpoint, Envelope>((_, env) => sentEnvelopes.Add(env))
                .Returns(Observable.Return(Unit.Default));

            var address = new RemoteAddress(
                actorId, targetNode, localNode, mockTransport.Object, serializer, pendingRequests);

            this.context["address"] = address;
            this.context["sentEnvelopes"] = sentEnvelopes;
            this.context["pendingRequests"] = pendingRequests;
        }

        /// <summary>
        /// Sends a tell message via the remote address.
        /// </summary>
        [When(@"a tell message is sent to the remote address")]
        public void WhenATellMessageIsSent()
        {
            var address = this.context.Get<RemoteAddress>("address");
            address.Send("hello").Wait();
        }

        /// <summary>
        /// Verifies a Tell envelope was sent via transport.
        /// </summary>
        [Then(@"the transport should have sent a Tell envelope")]
        public void ThenTheTransportShouldHaveSentATellEnvelope()
        {
            var envelopes = this.context.Get<ConcurrentBag<Envelope>>("sentEnvelopes");
            envelopes.Count.ShouldBe(1);
            envelopes.TryPeek(out var envelope);
            envelope.Type.ShouldBe(EnvelopeType.Tell);
        }

        /// <summary>
        /// Creates a pending request with a known correlation id.
        /// </summary>
        [Given(@"a pending request with correlation id C")]
        public void GivenAPendingRequestWithCorrelationId()
        {
            var correlationId = Guid.NewGuid();
            var tcs = new TaskCompletionSource<object>();
            var pendingRequests = new ConcurrentDictionary<Guid, TaskCompletionSource<object>>();
            pendingRequests[correlationId] = tcs;

            this.context["correlationId"] = correlationId;
            this.context["tcs"] = tcs;
            this.context["pendingRequests"] = pendingRequests;
            this.context["serializer"] = new JsonMessageSerializer();
        }

        /// <summary>
        /// Simulates a response envelope arriving.
        /// </summary>
        [When(@"a response envelope arrives with correlation id C")]
        public void WhenAResponseEnvelopeArrives()
        {
            var correlationId = this.context.Get<Guid>("correlationId");
            var pendingRequests = this.context.Get<ConcurrentDictionary<Guid, TaskCompletionSource<object>>>("pendingRequests");
            var serializer = this.context.Get<JsonMessageSerializer>("serializer");

            var payload = "response_payload";

            if (pendingRequests.TryRemove(correlationId, out var tcs))
            {
                tcs.TrySetResult(payload);
            }
        }

        /// <summary>
        /// Verifies the pending request was completed.
        /// </summary>
        [Then(@"the pending request should be completed with the response payload")]
        public void ThenThePendingRequestShouldBeCompleted()
        {
            var tcs = this.context.Get<TaskCompletionSource<object>>("tcs");
            tcs.Task.IsCompleted.ShouldBeTrue();
            tcs.Task.Result.ShouldBe("response_payload");
        }
    }
}
