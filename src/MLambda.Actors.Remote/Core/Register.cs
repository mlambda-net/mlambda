// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Register.cs" company="MLambda">
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

namespace MLambda.Actors.Remote.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Remote.Abstraction;

    /// <summary>
    /// Dependency injection registration for remote actor services.
    /// </summary>
    public static class Register
    {
        /// <summary>
        /// Adds remote actor services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddRemoteActor(this IServiceCollection services)
        {
            services.AddSingleton<IActorRegistry, ActorRegistry>();
            services.AddSingleton<ConcurrentDictionary<Guid, TaskCompletionSource<object>>>();
            services.AddSingleton<IAddressResolver, AddressResolver>();
            services.AddSingleton<IRemoteMessageDispatcher, RemoteMessageDispatcher>();
            return services;
        }
    }
}
