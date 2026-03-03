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

namespace MLambda.Actors.Cluster.Core
{
    using MLambda.Actors.Cluster.Abstraction;
    using MLambda.Actors.Gossip.Data;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// DI registration extensions for the cluster system.
    /// </summary>
    public static class Register
    {
        /// <summary>
        /// Registers cluster services and system actors with the DI container.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCluster(this IServiceCollection services)
        {
            services.AddSingleton<GossipDataReplicator>();
            services.AddSingleton<IClusterService, ClusterService>();
            services.AddTransient<RouteActor>();
            services.AddTransient<StateActor>();
            services.AddTransient<DeliveryActor>();
            return services;
        }
    }
}
