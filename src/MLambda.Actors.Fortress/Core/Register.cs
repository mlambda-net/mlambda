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

namespace MLambda.Actors.Fortress.Core
{
    using Microsoft.Extensions.DependencyInjection;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Dependency injection registration for Fortress mTLS services.
    /// </summary>
    public static class Register
    {
        /// <summary>
        /// Adds Fortress security services for Cluster/Hybrid nodes.
        /// Spawns CA actors and manages certificate lifecycle.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="config">The fortress configuration.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddFortress(
            this IServiceCollection services,
            FortressConfig config)
        {
            services.AddSingleton(config);
            services.AddSingleton<FortressTlsProvider>();
            services.AddSingleton<ITlsProvider>(p => p.GetRequiredService<FortressTlsProvider>());
            services.AddSingleton<FortressClock>();
            services.AddTransient<SentinelActor>();
            services.AddTransient<FortressAuthorizer>();
            services.AddTransient<GateKeeperActor>();
            services.AddSingleton<FortressService>();
            return services;
        }

        /// <summary>
        /// Adds Fortress client services for Satellite/Asteroid nodes.
        /// Requests certificates from the cluster's sentinel.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="config">The fortress configuration.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddFortressClient(
            this IServiceCollection services,
            FortressConfig config)
        {
            services.AddSingleton(config);
            services.AddSingleton<FortressTlsProvider>();
            services.AddSingleton<ITlsProvider>(p => p.GetRequiredService<FortressTlsProvider>());
            services.AddSingleton<FortressClientService>();
            return services;
        }
    }
}
