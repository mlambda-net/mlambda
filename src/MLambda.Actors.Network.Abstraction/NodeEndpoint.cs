// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NodeEndpoint.cs" company="MLambda">
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

    /// <summary>
    /// Represents a network endpoint for a node in the actor cluster.
    /// The NodeId serves as both the unique identifier and the hostname.
    /// </summary>
    public sealed class NodeEndpoint : IEquatable<NodeEndpoint>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NodeEndpoint"/> class.
        /// </summary>
        /// <param name="nodeId">The friendly node name, also used as the hostname.</param>
        /// <param name="port">The port number.</param>
        public NodeEndpoint(string nodeId, int port)
        {
            this.NodeId = nodeId;
            this.Port = port;
        }

        /// <summary>
        /// Gets the unique node identifier (also the hostname).
        /// </summary>
        public string NodeId { get; }

        /// <summary>
        /// Gets the port number.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Determines whether two NodeEndpoint instances are equal.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>True if equal.</returns>
        public static bool operator ==(NodeEndpoint left, NodeEndpoint right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two NodeEndpoint instances are not equal.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>True if not equal.</returns>
        public static bool operator !=(NodeEndpoint left, NodeEndpoint right)
        {
            return !Equals(left, right);
        }

        /// <inheritdoc/>
        public bool Equals(NodeEndpoint other)
        {
            if (other is null)
            {
                return false;
            }

            return string.Equals(this.NodeId, other.NodeId, StringComparison.Ordinal)
                && this.Port == other.Port;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as NodeEndpoint);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(StringComparer.Ordinal.GetHashCode(this.NodeId), this.Port);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.NodeId}:{this.Port}";
        }
    }
}
