// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ITlsProvider.cs" company="MLambda">
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

namespace MLambda.Actors.Network.Abstraction
{
    using System;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Provides TLS certificates and validation for mutual TLS (mTLS) connections.
    /// Consumed by <see cref="ITransport"/> implementations to upgrade TCP streams.
    /// </summary>
    public interface ITlsProvider
    {
        /// <summary>
        /// Gets a value indicating whether TLS is enabled and certificates are available.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Raised when certificates have been updated (initial issue or rotation).
        /// Listeners should close existing connections to force TLS renegotiation.
        /// </summary>
        event Action CertificatesUpdated;

        /// <summary>
        /// Gets the server certificate used for TLS server authentication.
        /// </summary>
        /// <returns>The server X.509 certificate with private key.</returns>
        X509Certificate2 GetServerCertificate();

        /// <summary>
        /// Gets the client certificate used for TLS client authentication.
        /// </summary>
        /// <returns>The client X.509 certificate with private key.</returns>
        X509Certificate2 GetClientCertificate();

        /// <summary>
        /// Gets the CA certificate used to validate remote certificates.
        /// </summary>
        /// <returns>The CA X.509 certificate.</returns>
        X509Certificate2 GetCaCertificate();

        /// <summary>
        /// Validates a remote certificate against the trusted CA.
        /// </summary>
        /// <param name="remoteCert">The remote certificate to validate.</param>
        /// <returns>True if the certificate is valid and trusted.</returns>
        bool ValidateRemoteCertificate(X509Certificate2 remoteCert);
    }
}
