// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FortressMessages.cs" company="MLambda">
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
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Request for a node certificate from the Fortress CA.
    /// </summary>
    public class CertificateRequest
    {
        /// <summary>
        /// Gets or sets the requesting node identifier.
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Gets or sets the requesting node endpoint.
        /// </summary>
        public NodeEndpoint RequestorEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the API key for external node authentication.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the source IP address for network detection.
        /// </summary>
        public string SourceIpAddress { get; set; }

        /// <summary>
        /// Gets or sets the nonce for replay protection.
        /// </summary>
        public byte[] Nonce { get; set; }
    }

    /// <summary>
    /// Response containing a signed node certificate from the Fortress CA.
    /// </summary>
    public class CertificateResponse
    {
        /// <summary>
        /// Gets or sets the node identifier this certificate was issued to.
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the request was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the signed certificate as PFX bytes.
        /// </summary>
        public byte[] CertificatePfx { get; set; }

        /// <summary>
        /// Gets or sets the CA certificate bytes for chain validation.
        /// </summary>
        public byte[] CaCertificateBytes { get; set; }

        /// <summary>
        /// Gets or sets the PFX password.
        /// </summary>
        public string PfxPassword { get; set; }

        /// <summary>
        /// Gets or sets the certificate expiration time.
        /// </summary>
        public DateTimeOffset ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the error message if the request failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the nonce echoed from the request.
        /// </summary>
        public byte[] Nonce { get; set; }
    }

    /// <summary>
    /// Request to create a new API key for external node access.
    /// </summary>
    public class ApiKeyCreateRequest
    {
        /// <summary>
        /// Gets or sets the label for the API key.
        /// </summary>
        public string Label { get; set; }
    }

    /// <summary>
    /// Response containing a newly created API key.
    /// </summary>
    public class ApiKeyCreateResponse
    {
        /// <summary>
        /// Gets or sets the generated API key.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether creation was successful.
        /// </summary>
        public bool Success { get; set; }
    }

    /// <summary>
    /// Request to validate an API key.
    /// </summary>
    public class ApiKeyValidation
    {
        /// <summary>
        /// Gets or sets the API key to validate.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the node identifier requesting validation.
        /// </summary>
        public string NodeId { get; set; }
    }

    /// <summary>
    /// Result of an API key validation.
    /// </summary>
    public class ApiKeyValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the API key is valid.
        /// </summary>
        public bool Valid { get; set; }
    }

    /// <summary>
    /// Notice sent to nodes when certificates are about to rotate.
    /// </summary>
    public class CertificateRotationNotice
    {
        /// <summary>
        /// Gets or sets the new expiration time after rotation.
        /// </summary>
        public DateTimeOffset NewExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the deadline by which nodes must have renewed.
        /// </summary>
        public DateTimeOffset RotationDeadline { get; set; }
    }

    /// <summary>
    /// Tick message from the FortressClock to trigger certificate rotation.
    /// </summary>
    public class RotationTick
    {
    }

    /// <summary>
    /// Stored certificate entry in the gossip-replicated dictionary.
    /// </summary>
    public class StoredCertificate
    {
        /// <summary>
        /// Gets or sets the node identifier this certificate belongs to.
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Gets or sets the PFX certificate bytes.
        /// </summary>
        public byte[] CertificatePfx { get; set; }

        /// <summary>
        /// Gets or sets the CA certificate bytes.
        /// </summary>
        public byte[] CaCertificateBytes { get; set; }

        /// <summary>
        /// Gets or sets the PFX password.
        /// </summary>
        public string PfxPassword { get; set; }

        /// <summary>
        /// Gets or sets when the certificate was issued.
        /// </summary>
        public DateTimeOffset IssuedAt { get; set; }

        /// <summary>
        /// Gets or sets when the certificate expires.
        /// </summary>
        public DateTimeOffset ExpiresAt { get; set; }
    }

    /// <summary>
    /// API key entry stored in the gossip-replicated dictionary.
    /// </summary>
    public class ApiKeyEntry
    {
        /// <summary>
        /// Gets or sets the API key value.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the label for the API key.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets when the key was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the key is revoked.
        /// </summary>
        public bool Revoked { get; set; }
    }
}
