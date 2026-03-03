// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FortressConfig.cs" company="MLambda">
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

    /// <summary>
    /// Configuration for the Fortress mTLS security system.
    /// Parsed from environment variables.
    /// </summary>
    public class FortressConfig
    {
        /// <summary>
        /// Gets or sets a value indicating whether Fortress mode is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the cluster name used as the CA certificate CN.
        /// </summary>
        public string ClusterName { get; set; } = "mlambda-cluster";

        /// <summary>
        /// Gets or sets the cron expression for certificate rotation.
        /// </summary>
        public string RotationCron { get; set; }

        /// <summary>
        /// Gets or sets the certificate validity duration.
        /// </summary>
        public TimeSpan CertificateValidity { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Gets or sets the API key for external node authentication.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the pre-shared secret for bootstrap encryption.
        /// If not set, auto-derived from ClusterName for same-network nodes.
        /// </summary>
        public string Secret { get; set; }

        /// <summary>
        /// Creates a <see cref="FortressConfig"/> from environment variables.
        /// </summary>
        /// <returns>A configured <see cref="FortressConfig"/> instance.</returns>
        public static FortressConfig FromEnvironment()
        {
            var enabled = string.Equals(
                Environment.GetEnvironmentVariable("FORTRESS"),
                "true",
                StringComparison.OrdinalIgnoreCase);

            var config = new FortressConfig
            {
                Enabled = enabled,
                ClusterName = Environment.GetEnvironmentVariable("FORTRESS_CLUSTER") ?? "mlambda-cluster",
                RotationCron = Environment.GetEnvironmentVariable("FORTRESS_CLOCK"),
                ApiKey = Environment.GetEnvironmentVariable("FORTRESS_API_KEY"),
                Secret = Environment.GetEnvironmentVariable("FORTRESS_SECRET"),
            };

            return config;
        }

        /// <summary>
        /// Gets the effective pre-shared key bytes for bootstrap encryption.
        /// Uses the explicit secret if set, otherwise derives from ClusterName.
        /// </summary>
        /// <returns>The PSK bytes for AES-256-GCM encryption.</returns>
        public byte[] GetPskBytes()
        {
            var secret = !string.IsNullOrWhiteSpace(this.Secret)
                ? this.Secret
                : this.ClusterName + "fortress-psk";

            return FortressCrypto.DeriveKey(secret);
        }
    }
}
