// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnvelopeCodec.cs" company="MLambda">
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

namespace MLambda.Actors.Network
{
    using System;
    using System.IO;
    using System.Text;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Static helper for serializing and deserializing envelopes for wire transmission.
    /// </summary>
    public static class EnvelopeCodec
    {
        /// <summary>
        /// Serializes an envelope to bytes for wire transmission.
        /// </summary>
        /// <param name="envelope">The envelope to serialize.</param>
        /// <returns>The serialized bytes.</returns>
        public static byte[] Encode(Envelope envelope)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            writer.Write(envelope.CorrelationId.ToByteArray());
            writer.Write(envelope.TargetActorId.ToByteArray());
            writer.Write(envelope.SourceActorId.ToByteArray());

            writer.Write(envelope.SourceNode.NodeId.ToByteArray());
            writer.Write(envelope.SourceNode.Host);
            writer.Write(envelope.SourceNode.Port);

            writer.Write((int)envelope.Type);
            writer.Write(envelope.PayloadTypeName ?? string.Empty);

            var payloadBytes = envelope.PayloadBytes ?? Array.Empty<byte>();
            writer.Write(payloadBytes.Length);
            writer.Write(payloadBytes);

            writer.Flush();
            return stream.ToArray();
        }

        /// <summary>
        /// Deserializes bytes back into an envelope.
        /// </summary>
        /// <param name="data">The serialized bytes.</param>
        /// <returns>The deserialized envelope.</returns>
        public static Envelope Decode(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            var correlationId = new Guid(reader.ReadBytes(16));
            var targetActorId = new Guid(reader.ReadBytes(16));
            var sourceActorId = new Guid(reader.ReadBytes(16));

            var sourceNodeId = new Guid(reader.ReadBytes(16));
            var sourceHost = reader.ReadString();
            var sourcePort = reader.ReadInt32();

            var type = (EnvelopeType)reader.ReadInt32();
            var payloadTypeName = reader.ReadString();

            var payloadLength = reader.ReadInt32();
            var payloadBytes = reader.ReadBytes(payloadLength);

            return new Envelope
            {
                CorrelationId = correlationId,
                TargetActorId = targetActorId,
                SourceActorId = sourceActorId,
                SourceNode = new NodeEndpoint(sourceNodeId, sourceHost, sourcePort),
                Type = type,
                PayloadTypeName = payloadTypeName,
                PayloadBytes = payloadBytes,
            };
        }
    }
}
