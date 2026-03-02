// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PortAllocator.cs" company="MLambda">
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

namespace MLambda.Actors.Gossip.Test
{
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    /// Allocates unique ports for test transports to avoid conflicts.
    /// </summary>
    public static class PortAllocator
    {
        /// <summary>
        /// Gets the next available port by binding to port 0.
        /// </summary>
        /// <returns>An available port number.</returns>
        public static int GetNextPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
