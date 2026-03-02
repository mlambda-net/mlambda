// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Envelope.cs" company="MLambda">
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
    /// Network message wrapper for transporting actor messages across nodes.
    /// </summary>
    public class Envelope
    {
        /// <summary>
        /// Gets or sets the correlation identifier for ask/response matching.
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the target actor identifier.
        /// </summary>
        public Guid TargetActorId { get; set; }

        /// <summary>
        /// Gets or sets the source actor identifier.
        /// </summary>
        public Guid SourceActorId { get; set; }

        /// <summary>
        /// Gets or sets the source node endpoint for response routing.
        /// </summary>
        public NodeEndpoint SourceNode { get; set; }

        /// <summary>
        /// Gets or sets the envelope type.
        /// </summary>
        public EnvelopeType Type { get; set; }

        /// <summary>
        /// Gets or sets the CLR type name for deserialization.
        /// </summary>
        public string PayloadTypeName { get; set; }

        /// <summary>
        /// Gets or sets the serialized payload bytes.
        /// </summary>
        public byte[] PayloadBytes { get; set; }
    }
}
