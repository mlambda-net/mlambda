// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FortressTlsProvider.cs" company="MLambda">
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
    using System.Security.Cryptography.X509Certificates;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Implements <see cref="ITlsProvider"/> for the Fortress mTLS system.
    /// Holds the current certificates and notifies listeners on update.
    /// </summary>
    public class FortressTlsProvider : ITlsProvider
    {
        private volatile X509Certificate2 serverCert;
        private volatile X509Certificate2 clientCert;
        private volatile X509Certificate2 caCert;

        /// <inheritdoc/>
        public bool IsEnabled => this.serverCert != null && this.caCert != null;

        /// <inheritdoc/>
        public event Action CertificatesUpdated;

        /// <inheritdoc/>
        public X509Certificate2 GetServerCertificate() => this.serverCert;

        /// <inheritdoc/>
        public X509Certificate2 GetClientCertificate() => this.clientCert;

        /// <inheritdoc/>
        public X509Certificate2 GetCaCertificate() => this.caCert;

        /// <inheritdoc/>
        public bool ValidateRemoteCertificate(X509Certificate2 remoteCert)
        {
            if (this.caCert == null || remoteCert == null)
            {
                return false;
            }

            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            chain.ChainPolicy.CustomTrustStore.Add(this.caCert);
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

            return chain.Build(remoteCert);
        }

        /// <summary>
        /// Updates the certificates and notifies all listeners.
        /// Called when certificates are received from the CA or during rotation.
        /// </summary>
        /// <param name="server">The server certificate with private key.</param>
        /// <param name="client">The client certificate with private key.</param>
        /// <param name="ca">The CA certificate for validation.</param>
        public void UpdateCertificates(X509Certificate2 server, X509Certificate2 client, X509Certificate2 ca)
        {
            this.serverCert = server;
            this.clientCert = client;
            this.caCert = ca;

            this.CertificatesUpdated?.Invoke();
        }
    }
}
