// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FortressService.cs" company="MLambda">
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
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Satellite.Abstraction;

    /// <summary>
    /// Cluster-side Fortress lifecycle manager.
    /// Spawns security actors (SentinelActor, FortressAuthorizer, GateKeeperActor),
    /// requests a self-certificate, and starts the rotation clock.
    /// </summary>
    public class FortressService : IDisposable
    {
        private readonly ISystemContext systemContext;
        private readonly FortressConfig config;
        private readonly FortressTlsProvider tlsProvider;
        private readonly FortressClock clock;
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;
        private IDisposable messageSubscription;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FortressService"/> class.
        /// </summary>
        /// <param name="systemContext">The system context for spawning actors.</param>
        /// <param name="config">The fortress configuration.</param>
        /// <param name="tlsProvider">The TLS provider to update with certificates.</param>
        /// <param name="clock">The rotation clock.</param>
        /// <param name="transport">The transport layer.</param>
        /// <param name="serializer">The message serializer.</param>
        public FortressService(
            ISystemContext systemContext,
            FortressConfig config,
            FortressTlsProvider tlsProvider,
            FortressClock clock,
            ITransport transport,
            IMessageSerializer serializer)
        {
            this.systemContext = systemContext;
            this.config = config;
            this.tlsProvider = tlsProvider;
            this.clock = clock;
            this.transport = transport;
            this.serializer = serializer;
            this.SystemActors = new Dictionary<string, IAddress>();
        }

        /// <summary>
        /// Gets the spawned security actor addresses.
        /// </summary>
        public IDictionary<string, IAddress> SystemActors { get; }

        /// <summary>
        /// Starts the Fortress security system on a cluster node.
        /// </summary>
        public void Start()
        {
            // Spawn security actors.
            this.SystemActors["fortress-ca"] = this.systemContext.Spawn<FortressAuthorizer>().Wait();
            this.SystemActors["gatekeeper"] = this.systemContext.Spawn<GateKeeperActor>().Wait();
            this.SystemActors["sentinel"] = this.systemContext.Spawn<SentinelActor>().Wait();

            // Subscribe to incoming Fortress messages for CertificateResponse.
            this.messageSubscription = this.transport.IncomingMessages
                .Where(e => e.Type == EnvelopeType.Fortress)
                .Subscribe(this.HandleFortressMessage);

            // Self-request certificate.
            var nonce = new byte[16];
            RandomNumberGenerator.Fill(nonce);

            var request = new CertificateRequest
            {
                NodeId = this.transport.LocalEndpoint.NodeId,
                RequestorEndpoint = this.transport.LocalEndpoint,
                SourceIpAddress = "127.0.0.1",
                Nonce = nonce,
            };

            var envelope = new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetRoute = "sentinel",
                SourceNode = this.transport.LocalEndpoint,
                Type = EnvelopeType.Fortress,
                PayloadTypeName = this.serializer.GetTypeName(request),
                PayloadBytes = this.serializer.Serialize(request),
            };

            this.transport.Send(this.transport.LocalEndpoint, envelope)
                .Subscribe(_ => { }, ex => { });

            // Start rotation clock.
            this.clock.Start();
        }

        /// <summary>
        /// Stops the Fortress security system.
        /// </summary>
        public void Stop()
        {
            this.clock.Stop();
            this.messageSubscription?.Dispose();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes managed resources.
        /// </summary>
        /// <param name="disposing">True if disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed && disposing)
            {
                this.disposed = true;
                this.Stop();
            }
        }

        private void HandleFortressMessage(Envelope envelope)
        {
            try
            {
                var payload = this.serializer.Deserialize(envelope.PayloadBytes, envelope.PayloadTypeName);
                if (payload is CertificateResponse response && response.Success)
                {
                    this.InstallCertificates(response);
                }
            }
            catch (Exception)
            {
                // Deserialization failure; ignore.
            }
        }

        private void InstallCertificates(CertificateResponse response)
        {
            var nodeCert = new X509Certificate2(response.CertificatePfx, response.PfxPassword);
            var caCert = new X509Certificate2(response.CaCertificateBytes);

            this.tlsProvider.UpdateCertificates(nodeCert, nodeCert, caCert);
        }
    }
}
