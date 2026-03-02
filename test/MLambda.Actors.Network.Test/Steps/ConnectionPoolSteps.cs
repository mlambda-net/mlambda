// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectionPoolSteps.cs" company="MLambda">
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
    using MLambda.Actors.Abstraction.Core;
    using MLambda.Actors.Network.Abstraction;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for connection pool feature tests.
    /// </summary>
    [Binding]
    public class ConnectionPoolSteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPoolSteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public ConnectionPoolSteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates two transports with event streams.
        /// </summary>
        [Given(@"two TCP transports on different ports with event streams")]
        public void GivenTwoTcpTransportsOnDifferentPortsWithEventStreams()
        {
            var portA = PortAllocator.GetNextPort();
            var portB = PortAllocator.GetNextPort();

            var endpointA = new NodeEndpoint(Guid.NewGuid(), "127.0.0.1", portA);
            var endpointB = new NodeEndpoint(Guid.NewGuid(), "127.0.0.1", portB);

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
            this.context["connectionEstablished"] = false;
            this.context["connectionLost"] = false;

            eventStreamA.Subscribe<ConnectionEstablished>(_ => this.context["connectionEstablished"] = true);
            eventStreamA.Subscribe<ConnectionLost>(_ => this.context["connectionLost"] = true);
            eventStreamB.Subscribe<ConnectionEstablished>(_ => this.context["connectionEstablished"] = true);
            eventStreamB.Subscribe<ConnectionLost>(_ => this.context["connectionLost"] = true);
        }

        /// <summary>
        /// A tell envelope is sent from transport A to transport B.
        /// </summary>
        /// <returns>A task.</returns>
        [Given(@"a tell envelope is sent from transport A to transport B")]
        [When(@"a tell envelope is sent from transport A to transport B")]
        public async Task ATellEnvelopeIsSentFromTransportAToTransportB()
        {
            await this.SendTellEnvelope();
        }

        /// <summary>
        /// Verifies a ConnectionEstablished event was published.
        /// </summary>
        [Then(@"a ConnectionEstablished event should be published")]
        public void ThenAConnectionEstablishedEventShouldBePublished()
        {
            var established = this.context.Get<bool>("connectionEstablished");
            established.ShouldBeTrue();
        }

        /// <summary>
        /// Stops transport B.
        /// </summary>
        /// <returns>A task.</returns>
        [When(@"transport B is stopped")]
        public async Task WhenTransportBIsStopped()
        {
            var transportB = this.context.Get<TcpTransport>("transportB");
            await transportB.Stop();
            await Task.Delay(500);
        }

        /// <summary>
        /// Verifies a ConnectionLost event was eventually published.
        /// </summary>
        [Then(@"a ConnectionLost event should eventually be published")]
        public void ThenAConnectionLostEventShouldEventuallyBePublished()
        {
            var lost = this.context.Get<bool>("connectionLost");
            lost.ShouldBeTrue();
        }

        /// <summary>
        /// Stops and restarts transport B.
        /// </summary>
        /// <returns>A task.</returns>
        [When(@"transport B is stopped and restarted")]
        public async Task WhenTransportBIsStoppedAndRestarted()
        {
            var transportB = this.context.Get<TcpTransport>("transportB");
            await transportB.Stop();
            transportB.Dispose();
            await Task.Delay(300);

            var endpointB = this.context.Get<NodeEndpoint>("endpointB");
            var eventStreamB = this.context.Get<IEventStream>("eventStreamB");
            var newTransportB = new TcpTransport(endpointB, eventStreamB);
            await newTransportB.Start();
            await Task.Delay(200);

            var receivedEnvelopes = new BlockingCollection<Envelope>();
            newTransportB.IncomingMessages.Subscribe(e => receivedEnvelopes.Add(e));
            this.context["reconnectReceivedEnvelopes"] = receivedEnvelopes;
            this.context["transportB"] = newTransportB;
        }

        /// <summary>
        /// Another tell envelope is sent from transport A to transport B.
        /// </summary>
        /// <returns>A task.</returns>
        [When(@"another tell envelope is sent from transport A to transport B")]
        public async Task WhenAnotherTellEnvelopeIsSentFromTransportAToTransportB()
        {
            await this.SendTellEnvelope();
        }

        /// <summary>
        /// Transport B should receive the second envelope.
        /// </summary>
        [Then(@"transport B should receive the second envelope")]
        public void ThenTransportBShouldReceiveTheSecondEnvelope()
        {
            var receivedEnvelopes = this.context.Get<BlockingCollection<Envelope>>("reconnectReceivedEnvelopes");
            receivedEnvelopes.TryTake(out var received, TimeSpan.FromSeconds(5)).ShouldBeTrue("Timed out waiting for envelope");
            received.ShouldNotBeNull();
        }

        private async Task SendTellEnvelope()
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
                Type = EnvelopeType.Tell,
                PayloadTypeName = "System.String",
                PayloadBytes = Encoding.UTF8.GetBytes("test"),
            };

            await transportA.Send(endpointB, envelope);
            await Task.Delay(200);
        }
    }
}
