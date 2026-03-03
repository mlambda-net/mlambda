// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GateKeeperActor.cs" company="MLambda">
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
    using System.Reactive.Linq;
    using System.Security.Cryptography;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Gossip.Data;

    /// <summary>
    /// API key management actor for external node authentication.
    /// Stores API keys in a gossip-replicated <see cref="GDictionary{TKey,TValue}"/>.
    /// </summary>
    [Route("gatekeeper")]
    public class GateKeeperActor : Actor
    {
        private readonly GDictionary<string, ApiKeyEntry> apiKeyStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="GateKeeperActor"/> class.
        /// </summary>
        /// <param name="replicator">The CRDT replicator for gossip sync.</param>
        /// <param name="config">The fortress configuration.</param>
        public GateKeeperActor(GossipDataReplicator replicator, FortressConfig config)
        {
            var nodeId = "local";
            this.apiKeyStore = new GDictionary<string, ApiKeyEntry>(
                "fortress-api-keys",
                nodeId);

            replicator.Register("fortress-api-keys", this.apiKeyStore);
        }

        /// <inheritdoc/>
        protected override Behavior Receive(object data)
            => data switch
            {
                ApiKeyCreateRequest msg => Actor.Behavior<ApiKeyCreateResponse, ApiKeyCreateRequest>(
                    this.HandleCreateApiKey, msg),
                ApiKeyValidation msg => Actor.Behavior<ApiKeyValidationResult, ApiKeyValidation>(
                    this.HandleValidateApiKey, msg),
                _ => Actor.Ignore,
            };

        private IObservable<ApiKeyCreateResponse> HandleCreateApiKey(ApiKeyCreateRequest msg)
        {
            var keyBytes = new byte[32];
            RandomNumberGenerator.Fill(keyBytes);
            var apiKey = "fk_" + Convert.ToBase64String(keyBytes).Replace("+", string.Empty).Replace("/", string.Empty).Replace("=", string.Empty);

            this.apiKeyStore.Set(apiKey, new ApiKeyEntry
            {
                ApiKey = apiKey,
                Label = msg.Label,
                CreatedAt = DateTimeOffset.UtcNow,
                Revoked = false,
            });

            return Observable.Return(new ApiKeyCreateResponse
            {
                ApiKey = apiKey,
                Success = true,
            });
        }

        private IObservable<ApiKeyValidationResult> HandleValidateApiKey(ApiKeyValidation msg)
        {
            if (string.IsNullOrWhiteSpace(msg.ApiKey))
            {
                return Observable.Return(new ApiKeyValidationResult { Valid = false });
            }

            if (this.apiKeyStore.TryGet(msg.ApiKey, out var entry) && !entry.Revoked)
            {
                return Observable.Return(new ApiKeyValidationResult { Valid = true });
            }

            return Observable.Return(new ApiKeyValidationResult { Valid = false });
        }
    }
}
