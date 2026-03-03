// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TcpTransportSteps.cs" company="MLambda">
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

namespace MLambda.Actors.Network.Test.Steps
{
    using System;
    using System.Collections.Concurrent;
    using System.Reactive.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using MLambda.Actors.Network.Abstraction;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for TCP transport feature tests.
    /// </summary>
    [Binding]
    public class TcpTransportSteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpTransportSteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public TcpTransportSteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates two transports on different ports.
        /// </summary>
        [Given(@"two TCP transports on different ports")]
        public void GivenTwoTcpTransportsOnDifferentPorts()
        {
            var portA = PortAllocator.GetNextPort();
            var portB = PortAllocator.GetNextPort();

            var endpointA = new NodeEndpoint("127.0.0.1", portA);
            var endpointB = new NodeEndpoint("127.0.0.1", portB);

            var eventStreamA = new MLambda.Actors.EventStream();
            var eventStreamB = new MLambda.Actors.EventStream();

            var transportA = new TcpTransport(endpointA, eventStreamA);
            var transportB = new TcpTransport(endpointB, eventStreamB);

            this.context["transportA"] = transportA;
            this.context["transportB"] = transportB;
            this.context["endpointA"] = endpointA;
            this.context["endpointB"] = endpointB;
            this.context["eventStreamA"] = eventStreamA;
            this.context["eventStreamB"] = eventStreamB;
        }

        /// <summary>
        /// Starts both transports and subscribes to incoming messages.
        /// </summary>
        /// <returns>A task.</returns>
        [Given(@"both transports are started")]
        [When(@"both transports are started")]
        public async Task BothTransportsAreStarted()
        {
            var transportA = this.context.Get<TcpTransport>("transportA");
            var transportB = this.context.Get<TcpTransport>("transportB");

            await transportA.Start();
            await transportB.Start();

            var receivedEnvelopes = new BlockingCollection<Envelope>();
            transportB.IncomingMessages.Subscribe(e => receivedEnvelopes.Add(e));
            this.context["receivedEnvelopes"] = receivedEnvelopes;

            await Task.Delay(100);
        }

        /// <summary>
        /// Transport A sends a tell envelope to B.
        /// </summary>
        /// <returns>A task.</returns>
        [When(@"transport A sends a tell envelope to transport B")]
        public async Task WhenTransportASendsATellEnvelopeToTransportB()
        {
            await this.SendEnvelope(EnvelopeType.Tell, Encoding.UTF8.GetBytes("tell-payload"));
        }

        /// <summary>
        /// Transport A sends an ask envelope to B.
        /// </summary>
        /// <returns>A task.</returns>
        [When(@"transport A sends an ask envelope to transport B")]
        public async Task WhenTransportASendsAnAskEnvelopeToTransportB()
        {
            await this.SendEnvelope(EnvelopeType.Ask, Encoding.UTF8.GetBytes("ask-payload"));
        }

        /// <summary>
        /// Transport A sends a response envelope to B.
        /// </summary>
        /// <returns>A task.</returns>
        [When(@"transport A sends a response envelope to transport B")]
        public async Task WhenTransportASendsAResponseEnvelopeToTransportB()
        {
            await this.SendEnvelope(EnvelopeType.Response, Encoding.UTF8.GetBytes("response-payload"));
        }

        /// <summary>
        /// Transport A sends an envelope with specific payload to B.
        /// </summary>
        /// <param name="payload">The payload string.</param>
        /// <returns>A task.</returns>
        [When(@"transport A sends an envelope with payload ""(.*)"" to transport B")]
        public async Task WhenTransportASendsAnEnvelopeWithPayloadToTransportB(string payload)
        {
            var serializer = new JsonMessageSerializer();
            var bytes = serializer.Serialize(payload);
            var typeName = serializer.GetTypeName(payload);

            await this.SendEnvelope(EnvelopeType.Tell, bytes, typeName);
        }

        /// <summary>
        /// Transport B should receive the envelope.
        /// </summary>
        [Then(@"transport B should receive the envelope")]
        public void ThenTransportBShouldReceiveTheEnvelope()
        {
            var receivedEnvelopes = this.context.Get<BlockingCollection<Envelope>>("receivedEnvelopes");
            receivedEnvelopes.TryTake(out var received, TimeSpan.FromSeconds(5)).ShouldBeTrue("Timed out waiting for envelope");
            this.context["received_envelope"] = received;
        }

        /// <summary>
        /// Verifies the received envelope type.
        /// </summary>
        /// <param name="expectedType">The expected type.</param>
        [Then(@"the received envelope type should be ""(.*)""")]
        public void ThenTheReceivedEnvelopeTypeShouldBe(string expectedType)
        {
            var received = this.context.Get<Envelope>("received_envelope");
            received.Type.ShouldBe(Enum.Parse<EnvelopeType>(expectedType));
        }

        /// <summary>
        /// Verifies the received payload.
        /// </summary>
        [Then(@"transport B should receive the envelope with the correct payload")]
        public void ThenTransportBShouldReceiveTheEnvelopeWithTheCorrectPayload()
        {
            var receivedEnvelopes = this.context.Get<BlockingCollection<Envelope>>("receivedEnvelopes");
            receivedEnvelopes.TryTake(out var received, TimeSpan.FromSeconds(5)).ShouldBeTrue("Timed out waiting for envelope");

            var serializer = new JsonMessageSerializer();
            var deserialized = (string)serializer.Deserialize(received.PayloadBytes, received.PayloadTypeName);
            deserialized.ShouldBe("TestPayload");
        }

        /// <summary>
        /// Cleanup transports after each scenario.
        /// </summary>
        /// <returns>A task.</returns>
        [AfterScenario]
        public async Task Cleanup()
        {
            if (this.context.TryGetValue("transportA", out TcpTransport transportA))
            {
                try
                {
                    await transportA.Stop();
                    transportA.Dispose();
                }
                catch
                {
                }
            }

            if (this.context.TryGetValue("transportB", out TcpTransport transportB))
            {
                try
                {
                    await transportB.Stop();
                    transportB.Dispose();
                }
                catch
                {
                }
            }
        }

        private async Task SendEnvelope(EnvelopeType type, byte[] payloadBytes, string typeName = "System.String")
        {
            var transportA = this.context.Get<TcpTransport>("transportA");
            var endpointA = this.context.Get<NodeEndpoint>("endpointA");
            var endpointB = this.context.Get<NodeEndpoint>("endpointB");

            var envelope = new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetActorId = Guid.NewGuid(),
                SourceActorId = Guid.NewGuid(),
                SourceNode = endpointA,
                Type = type,
                PayloadTypeName = typeName,
                PayloadBytes = payloadBytes,
            };

            this.context["sent_envelope"] = envelope;
            await transportA.Send(endpointB, envelope);
            await Task.Delay(200);
        }
    }
}
