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

namespace MLambda.Actors.Monitoring.Core
{
    using System;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using MLambda.Actors.Abstraction.Supervision;
    using MLambda.Actors.Supervision;
    using OpenTelemetry;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace;

    /// <summary>
    /// Dependency injection registration for actor monitoring.
    /// </summary>
    public static class Register
    {
        /// <summary>
        /// Adds transparent actor monitoring with Prometheus metrics, OpenTelemetry
        /// distributed tracing, and structured console logging.
        /// Must be called after <c>AddActor()</c>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddActorMonitoring(
            this IServiceCollection services,
            Action<MonitoringConfig> configure = null)
        {
            var config = new MonitoringConfig();
            configure?.Invoke(config);

            var existingDescriptor = services.LastOrDefault(d => d.ServiceType == typeof(ISupervisor));

            if (existingDescriptor != null)
            {
                services.Remove(existingDescriptor);
            }

            services.AddSingleton<ISupervisor>(provider =>
            {
                ISupervisor inner;
                if (existingDescriptor?.ImplementationInstance != null)
                {
                    inner = (ISupervisor)existingDescriptor.ImplementationInstance;
                }
                else if (existingDescriptor?.ImplementationFactory != null)
                {
                    inner = (ISupervisor)existingDescriptor.ImplementationFactory(provider);
                }
                else
                {
                    inner = Strategy.OneForOne(decider => decider.Default(Directive.Resume));
                }

                return new InstrumentedSupervisor(inner, config);
            });

            if (config.EnableTracing)
            {
                services.AddOpenTelemetry()
                    .ConfigureResource(resource => resource
                        .AddService(
                            serviceName: "MLambda.Actors",
                            serviceInstanceId: config.NodeId))
                    .WithTracing(builder => builder
                        .AddSource(ActorTracing.SourceName)
                        .AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(config.OtlpEndpoint);
                        }));
            }

            return services;
        }
    }
}
