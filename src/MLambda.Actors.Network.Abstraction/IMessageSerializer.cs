// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IMessageSerializer.cs" company="MLambda">
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
    /// <summary>
    /// Serializes and deserializes actor messages for network transport.
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// Serializes a message object to bytes.
        /// </summary>
        /// <param name="message">The message to serialize.</param>
        /// <returns>The serialized bytes.</returns>
        byte[] Serialize(object message);

        /// <summary>
        /// Deserializes bytes to a message object.
        /// </summary>
        /// <param name="data">The serialized bytes.</param>
        /// <param name="typeName">The CLR type name.</param>
        /// <returns>The deserialized message.</returns>
        object Deserialize(byte[] data, string typeName);

        /// <summary>
        /// Gets the CLR type name for a message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The type name.</returns>
        string GetTypeName(object message);
    }
}
