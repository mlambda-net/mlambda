// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FortressClientService.cs" company="MLambda">
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
    using System.Linq;
    using System.Reactive.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Satellite.Abstraction;

    /// <summary>
    /// Satellite/Asteroid-side Fortress client.
    /// Requests certificates from the cluster's sentinel and installs them locally.
    /// Handles proactive renewal at 90% of certificate validity.
    /// </summary>
    public class FortressClientService : IDisposable
    {
        private readonly ActorCatalogConfig catalogConfig;
        private readonly FortressConfig config;
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;
        private readonly FortressTlsProvider tlsProvider;
        private IDisposable messageSubscription;
        private Timer renewalTimer;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FortressClientService"/> class.
        /// </summary>
        /// <param name="catalogConfig">The actor catalog configuration.</param>
        /// <param name="config">The fortress configuration.</param>
        /// <param name="transport">The transport layer.</param>
        /// <param name="serializer">The message serializer.</param>
        /// <param name="tlsProvider">The TLS provider to update with certificates.</param>
        public FortressClientService(
            ActorCatalogConfig catalogConfig,
            FortressConfig config,
            ITransport transport,
            IMessageSerializer serializer,
            FortressTlsProvider tlsProvider)
        {
            this.catalogConfig = catalogConfig;
            this.config = config;
            this.transport = transport;
            this.serializer = serializer;
            this.tlsProvider = tlsProvider;
        }

        /// <summary>
        /// Starts the Fortress client by requesting a certificate from the cluster.
        /// </summary>
        public void Start()
        {
            // Subscribe to incoming Fortress messages for CertificateResponse.
            this.messageSubscription = this.transport.IncomingMessages
                .Where(e => e.Type == EnvelopeType.Fortress)
                .Subscribe(this.HandleFortressMessage);

            this.RequestCertificate();
        }

        /// <summary>
        /// Stops the Fortress client.
        /// </summary>
        public void Stop()
        {
            this.renewalTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            this.renewalTimer?.Dispose();
            this.renewalTimer = null;
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

        private void RequestCertificate()
        {
            var clusterTarget = this.catalogConfig.ClusterNodes?.FirstOrDefault();
            if (clusterTarget == null)
            {
                return;
            }

            var nonce = new byte[16];
            RandomNumberGenerator.Fill(nonce);

            var request = new CertificateRequest
            {
                NodeId = this.transport.LocalEndpoint.NodeId,
                RequestorEndpoint = this.transport.LocalEndpoint,
                ApiKey = this.config.ApiKey,
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

            this.transport.Send(clusterTarget, envelope)
                .Subscribe(_ => { }, ex => { });
        }

        private void HandleFortressMessage(Envelope envelope)
        {
            try
            {
                var payload = this.serializer.Deserialize(envelope.PayloadBytes, envelope.PayloadTypeName);
                if (payload is CertificateResponse response && response.Success)
                {
                    this.InstallCertificates(response);
                    this.ScheduleRenewal(response.ExpiresAt);
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

        private void ScheduleRenewal(DateTimeOffset expiresAt)
        {
            var validity = expiresAt - DateTimeOffset.UtcNow;
            var renewalDelay = TimeSpan.FromTicks((long)(validity.Ticks * 0.9));

            if (renewalDelay < TimeSpan.FromSeconds(10))
            {
                renewalDelay = TimeSpan.FromSeconds(10);
            }

            this.renewalTimer?.Dispose();
            this.renewalTimer = new Timer(
                _ => this.RequestCertificate(),
                null,
                renewalDelay,
                Timeout.InfiniteTimeSpan);
        }
    }
}
