// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JsonMessageSerializer.cs" company="MLambda">
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
    using System.Text.Json;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// JSON-based message serializer using System.Text.Json.
    /// </summary>
    public class JsonMessageSerializer : IMessageSerializer
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        /// <inheritdoc/>
        public byte[] Serialize(object message)
        {
            return JsonSerializer.SerializeToUtf8Bytes(message, message.GetType(), Options);
        }

        /// <inheritdoc/>
        public object Deserialize(byte[] data, string typeName)
        {
            var type = Type.GetType(typeName);
            if (type == null)
            {
                throw new InvalidOperationException($"Cannot resolve type: {typeName}");
            }

            return JsonSerializer.Deserialize(data, type, Options);
        }

        /// <inheritdoc/>
        public string GetTypeName(object message)
        {
            return message.GetType().AssemblyQualifiedName;
        }
    }
}
