// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AddressResolver.cs" company="MLambda">
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

namespace MLambda.Actors.Satellite
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading.Tasks;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Core;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Satellite.Abstraction;

    /// <summary>
    /// Resolves actor addresses to local or remote implementations.
    /// </summary>
    public class AddressResolver : IAddressResolver
    {
        private readonly IBucket bucket;
        private readonly IActorRegistry registry;
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<object>> pendingRequests;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddressResolver"/> class.
        /// </summary>
        /// <param name="bucket">The local actor container.</param>
        /// <param name="registry">The actor registry.</param>
        /// <param name="transport">The transport layer.</param>
        /// <param name="serializer">The message serializer.</param>
        /// <param name="pendingRequests">Shared pending requests for response correlation.</param>
        public AddressResolver(
            IBucket bucket,
            IActorRegistry registry,
            ITransport transport,
            IMessageSerializer serializer,
            ConcurrentDictionary<Guid, TaskCompletionSource<object>> pendingRequests)
        {
            this.bucket = bucket;
            this.registry = registry;
            this.transport = transport;
            this.serializer = serializer;
            this.pendingRequests = pendingRequests;
        }

        /// <inheritdoc/>
        public IAddress Resolve(Guid actorId)
        {
            var localProcess = this.bucket.Filter(p => p.Id == actorId).FirstOrDefault();
            if (localProcess?.Current?.Address != null)
            {
                return localProcess.Current.Address;
            }

            var node = this.registry.Lookup(actorId);
            if (node != null)
            {
                return new RemoteAddress(
                    actorId,
                    node,
                    this.transport.LocalEndpoint,
                    this.transport,
                    this.serializer,
                    this.pendingRequests);
            }

            return null;
        }
    }
}
