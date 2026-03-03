// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FortressCrypto.cs" company="MLambda">
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
    using System.Security.Cryptography;

    /// <summary>
    /// AES-256-GCM encryption utilities for pre-shared key bootstrap.
    /// Used to protect certificate request/response payloads before mTLS is established.
    /// </summary>
    public static class FortressCrypto
    {
        private const int NonceSize = 12;
        private const int TagSize = 16;

        /// <summary>
        /// Derives a 32-byte AES key from a secret string using SHA-256.
        /// </summary>
        /// <param name="secret">The secret string.</param>
        /// <returns>A 32-byte key suitable for AES-256.</returns>
        public static byte[] DeriveKey(string secret)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(secret));
        }

        /// <summary>
        /// Encrypts plaintext using AES-256-GCM with the given key.
        /// Output format: [12-byte nonce][ciphertext][16-byte tag].
        /// </summary>
        /// <param name="plaintext">The data to encrypt.</param>
        /// <param name="key">The 32-byte AES key.</param>
        /// <returns>The encrypted data with nonce and tag.</returns>
        public static byte[] Encrypt(byte[] plaintext, byte[] key)
        {
            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            var result = new byte[NonceSize + ciphertext.Length + TagSize];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(ciphertext, 0, result, NonceSize, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, NonceSize + ciphertext.Length, TagSize);

            return result;
        }

        /// <summary>
        /// Decrypts data previously encrypted with <see cref="Encrypt"/>.
        /// Expects format: [12-byte nonce][ciphertext][16-byte tag].
        /// </summary>
        /// <param name="encrypted">The encrypted data.</param>
        /// <param name="key">The 32-byte AES key.</param>
        /// <returns>The decrypted plaintext.</returns>
        public static byte[] Decrypt(byte[] encrypted, byte[] key)
        {
            if (encrypted.Length < NonceSize + TagSize)
            {
                throw new CryptographicException("Encrypted data is too short.");
            }

            var nonce = new byte[NonceSize];
            Buffer.BlockCopy(encrypted, 0, nonce, 0, NonceSize);

            var ciphertextLength = encrypted.Length - NonceSize - TagSize;
            var ciphertext = new byte[ciphertextLength];
            Buffer.BlockCopy(encrypted, NonceSize, ciphertext, 0, ciphertextLength);

            var tag = new byte[TagSize];
            Buffer.BlockCopy(encrypted, NonceSize + ciphertextLength, tag, 0, TagSize);

            var plaintext = new byte[ciphertextLength];
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return plaintext;
        }
    }
}
