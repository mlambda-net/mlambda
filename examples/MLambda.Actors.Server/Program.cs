// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="MLambda">
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

namespace MLambda.Actors.Server
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using MLambda.Actors.Remote;
    using MLambda.Actors.Remote.Abstraction;
    using Prometheus;

    /// <summary>
    /// The actor server node entry point.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>An async task.</returns>
        public static async Task Main(string[] args)
        {
            var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? "node-1";
            var metricsPort = int.Parse(Environment.GetEnvironmentVariable("METRICS_PORT") ?? "9100");
            var nodeType = ActorAddressConfig.ParseNodeType(Environment.GetEnvironmentVariable("NODE_TYPE"));

            var node = ActorAddress.Build(config =>
            {
                config.NodeId = nodeId;
                config.Port = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "5000");
                config.NodeType = nodeType;
                config.SeedNodes = ActorAddressConfig.ParseSeedNodes(Environment.GetEnvironmentVariable("SEED_NODES"));
                config.ClusterNodes = ActorAddressConfig.ParseClusterNodes(Environment.GetEnvironmentVariable("CLUSTER_NODES"));
                config.OtlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";
                config.Register<GreeterActor>();
                config.Register<CalculatorActor>();
            });

            Console.WriteLine($"[Node] Starting node {nodeId}");

            var metricServer = new MetricServer(port: metricsPort);
            metricServer.Start();
            await node.Start();

            Console.WriteLine("[Node] Node is ready. Actors will be created on demand. Press Ctrl+C to shut down.");

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (TaskCanceledException)
            {
            }

            await node.Stop();
            metricServer.Stop();
            Console.WriteLine("[Node] Node stopped.");
        }
    }
}
