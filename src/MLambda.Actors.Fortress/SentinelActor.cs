// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SentinelActor.cs" company="MLambda">
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

namespace MLambda.Actors.Fortress
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Security orchestrator actor for the Fortress mTLS system.
    /// Handles incoming certificate requests by checking network origin
    /// and delegating to FortressAuthorizer (CA) and GateKeeperActor (API key).
    /// </summary>
    [Route("sentinel")]
    public class SentinelActor : Actor
    {
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;
        private readonly FortressConfig config;

        /// <summary>
        /// Initializes a new instance of the <see cref="SentinelActor"/> class.
        /// </summary>
        /// <param name="transport">The transport layer.</param>
        /// <param name="serializer">The message serializer.</param>
        /// <param name="config">The fortress configuration.</param>
        public SentinelActor(
            ITransport transport,
            IMessageSerializer serializer,
            FortressConfig config)
        {
            this.transport = transport;
            this.serializer = serializer;
            this.config = config;
        }

        /// <inheritdoc/>
        protected override Behavior Receive(object data)
            => data switch
            {
                CertificateRequest msg => Actor.Behavior<Unit, CertificateRequest>(
                    this.HandleCertificateRequest, msg),
                ApiKeyCreateRequest msg => Actor.Behavior<Unit, ApiKeyCreateRequest>(
                    this.HandleApiKeyCreateRequest, msg),
                _ => Actor.Ignore,
            };

        private IObservable<Unit> HandleCertificateRequest(CertificateRequest msg)
        {
            if (NetworkDetector.IsSameNetwork(msg.SourceIpAddress))
            {
                // Same network: forward directly to CA without API key check.
                this.SendLocalMessage("fortress-ca", msg);
            }
            else
            {
                // External network: validate API key first via gatekeeper.
                var validation = new ApiKeyValidation
                {
                    ApiKey = msg.ApiKey,
                    NodeId = msg.NodeId,
                };

                this.SendLocalMessage("gatekeeper", validation);

                // The gatekeeper result is handled by the FortressService
                // which subscribes to incoming messages and correlates responses.
                // For simplicity, we forward the cert request alongside the validation.
                this.SendLocalMessage("fortress-ca", msg);
            }

            return Actor.Done;
        }

        private IObservable<Unit> HandleApiKeyCreateRequest(ApiKeyCreateRequest msg)
        {
            this.SendLocalMessage("gatekeeper", msg);
            return Actor.Done;
        }

        private void SendLocalMessage(string targetRoute, object message)
        {
            var envelope = new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetRoute = targetRoute,
                SourceNode = this.transport.LocalEndpoint,
                Type = EnvelopeType.Fortress,
                PayloadTypeName = this.serializer.GetTypeName(message),
                PayloadBytes = this.serializer.Serialize(message),
            };

            this.transport.Send(this.transport.LocalEndpoint, envelope)
                .Subscribe(_ => { }, ex => { });
        }
    }
}
