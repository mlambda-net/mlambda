// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FortressAuthorizer.cs" company="MLambda">
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
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Gossip.Data;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Certificate Authority actor that creates and signs X.509 certificates
    /// for cluster nodes. Stores issued certificates in a gossip-replicated
    /// <see cref="GDictionary{TKey,TValue}"/> for distribution.
    /// </summary>
    [Route("fortress-ca")]
    public class FortressAuthorizer : Actor
    {
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;
        private readonly FortressConfig config;
        private readonly GDictionary<string, StoredCertificate> certStore;

        private RSA caKey;
        private X509Certificate2 caCert;

        /// <summary>
        /// Initializes a new instance of the <see cref="FortressAuthorizer"/> class.
        /// </summary>
        /// <param name="transport">The transport layer.</param>
        /// <param name="serializer">The message serializer.</param>
        /// <param name="replicator">The CRDT replicator for gossip sync.</param>
        /// <param name="config">The fortress configuration.</param>
        public FortressAuthorizer(
            ITransport transport,
            IMessageSerializer serializer,
            GossipDataReplicator replicator,
            FortressConfig config)
        {
            this.transport = transport;
            this.serializer = serializer;
            this.config = config;
            this.certStore = new GDictionary<string, StoredCertificate>(
                "fortress-cert-store",
                transport.LocalEndpoint?.NodeId ?? "local");

            replicator.Register("fortress-cert-store", this.certStore);
        }

        /// <inheritdoc/>
        protected override Behavior Receive(object data)
            => data switch
            {
                CertificateRequest msg => Actor.Behavior<Unit, CertificateRequest>(
                    this.HandleCertificateRequest, msg),
                RotationTick msg => Actor.Behavior<Unit, RotationTick>(
                    this.HandleRotationTick, msg),
                _ => Actor.Ignore,
            };

        private IObservable<Unit> HandleCertificateRequest(CertificateRequest msg)
        {
            try
            {
                this.EnsureCaInitialized();

                var nodeCert = this.CreateNodeCertificate(msg.NodeId);
                var pfxPassword = Guid.NewGuid().ToString("N");
                var pfxBytes = nodeCert.Export(X509ContentType.Pfx, pfxPassword);
                var caBytes = this.caCert.Export(X509ContentType.Cert);
                var expiresAt = DateTimeOffset.UtcNow.Add(this.config.CertificateValidity);

                // Store in gossip-replicated dictionary.
                this.certStore.Set(msg.NodeId, new StoredCertificate
                {
                    NodeId = msg.NodeId,
                    CertificatePfx = pfxBytes,
                    CaCertificateBytes = caBytes,
                    PfxPassword = pfxPassword,
                    IssuedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = expiresAt,
                });

                var response = new CertificateResponse
                {
                    NodeId = msg.NodeId,
                    Success = true,
                    CertificatePfx = pfxBytes,
                    CaCertificateBytes = caBytes,
                    PfxPassword = pfxPassword,
                    ExpiresAt = expiresAt,
                    Nonce = msg.Nonce,
                };

                this.SendResponse(msg.RequestorEndpoint, response);
            }
            catch (Exception ex)
            {
                var response = new CertificateResponse
                {
                    NodeId = msg.NodeId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    Nonce = msg.Nonce,
                };

                this.SendResponse(msg.RequestorEndpoint, response);
            }

            return Actor.Done;
        }

        private IObservable<Unit> HandleRotationTick(RotationTick msg)
        {
            try
            {
                // Regenerate CA and re-sign all certificates.
                this.GenerateCa();

                foreach (var kvp in this.certStore.GetAll())
                {
                    var stored = kvp.Value;
                    var nodeCert = this.CreateNodeCertificate(stored.NodeId);
                    var pfxPassword = Guid.NewGuid().ToString("N");
                    var pfxBytes = nodeCert.Export(X509ContentType.Pfx, pfxPassword);
                    var caBytes = this.caCert.Export(X509ContentType.Cert);
                    var expiresAt = DateTimeOffset.UtcNow.Add(this.config.CertificateValidity);

                    this.certStore.Set(stored.NodeId, new StoredCertificate
                    {
                        NodeId = stored.NodeId,
                        CertificatePfx = pfxBytes,
                        CaCertificateBytes = caBytes,
                        PfxPassword = pfxPassword,
                        IssuedAt = DateTimeOffset.UtcNow,
                        ExpiresAt = expiresAt,
                    });

                    // Notify each node to refresh its certificates.
                    if (stored.NodeId != this.transport.LocalEndpoint?.NodeId)
                    {
                        var response = new CertificateResponse
                        {
                            NodeId = stored.NodeId,
                            Success = true,
                            CertificatePfx = pfxBytes,
                            CaCertificateBytes = caBytes,
                            PfxPassword = pfxPassword,
                            ExpiresAt = expiresAt,
                        };

                        // Best-effort: send to the node if it's reachable.
                        var envelope = new Envelope
                        {
                            CorrelationId = Guid.NewGuid(),
                            TargetRoute = "sentinel",
                            SourceNode = this.transport.LocalEndpoint,
                            Type = EnvelopeType.Fortress,
                            PayloadTypeName = this.serializer.GetTypeName(response),
                            PayloadBytes = this.serializer.Serialize(response),
                        };

                        // Send to the stored node endpoint via its NodeId.
                        this.transport.Send(this.transport.LocalEndpoint, envelope)
                            .Subscribe(_ => { }, ex => { });
                    }
                }
            }
            catch (Exception)
            {
                // Rotation failure is logged but does not crash the actor.
            }

            return Actor.Done;
        }

        private void EnsureCaInitialized()
        {
            if (this.caCert == null)
            {
                this.GenerateCa();
            }
        }

        private void GenerateCa()
        {
            this.caKey?.Dispose();
            this.caKey = RSA.Create(4096);

            var caReq = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                $"CN={this.config.ClusterName} CA",
                this.caKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            caReq.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, false, 0, true));

            caReq.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                    true));

            var caValidity = this.config.CertificateValidity.Add(this.config.CertificateValidity);
            this.caCert = caReq.CreateSelfSigned(
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.Add(caValidity));
        }

        private X509Certificate2 CreateNodeCertificate(string nodeId)
        {
            using var nodeKey = RSA.Create(2048);

            var nodeReq = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                $"CN={nodeId}",
                nodeKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            nodeReq.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, false));

            nodeReq.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    true));

            nodeReq.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection
                    {
                        new Oid("1.3.6.1.5.5.7.3.1"), // Server Authentication
                        new Oid("1.3.6.1.5.5.7.3.2"), // Client Authentication
                    },
                    false));

            // Subject Alternative Names.
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(nodeId);
            sanBuilder.AddDnsName(this.config.ClusterName);
            nodeReq.CertificateExtensions.Add(sanBuilder.Build());

            var serial = new byte[16];
            RandomNumberGenerator.Fill(serial);

            var nodeCert = nodeReq.Create(
                this.caCert,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.Add(this.config.CertificateValidity),
                serial);

            return nodeCert.CopyWithPrivateKey(nodeKey);
        }

        private void SendResponse(NodeEndpoint target, CertificateResponse response)
        {
            if (target == null)
            {
                return;
            }

            var envelope = new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetRoute = "sentinel",
                SourceNode = this.transport.LocalEndpoint,
                Type = EnvelopeType.Fortress,
                PayloadTypeName = this.serializer.GetTypeName(response),
                PayloadBytes = this.serializer.Serialize(response),
            };

            this.transport.Send(target, envelope)
                .Subscribe(_ => { }, ex => { });
        }
    }
}
