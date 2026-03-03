// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NetworkDetector.cs" company="MLambda">
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
    using System.Net;

    /// <summary>
    /// Detects whether an IP address belongs to the same private network.
    /// Used by SentinelActor to determine if API key validation is required.
    /// </summary>
    public static class NetworkDetector
    {
        /// <summary>
        /// Determines whether the given IP address is on a private/same network.
        /// Private ranges: 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16, 127.0.0.0/8.
        /// Docker bridge networks (172.17+) are treated as private.
        /// </summary>
        /// <param name="ipAddress">The IP address string to check.</param>
        /// <returns>True if the address is on a private network.</returns>
        public static bool IsSameNetwork(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return false;
            }

            if (!IPAddress.TryParse(ipAddress, out var ip))
            {
                return false;
            }

            var bytes = ip.GetAddressBytes();

            if (bytes.Length != 4)
            {
                // IPv6 loopback.
                return IPAddress.IsLoopback(ip);
            }

            // 127.0.0.0/8 — loopback.
            if (bytes[0] == 127)
            {
                return true;
            }

            // 10.0.0.0/8 — Class A private.
            if (bytes[0] == 10)
            {
                return true;
            }

            // 172.16.0.0/12 — Class B private (includes Docker bridge 172.17+).
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return true;
            }

            // 192.168.0.0/16 — Class C private.
            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true;
            }

            return false;
        }
    }
}
