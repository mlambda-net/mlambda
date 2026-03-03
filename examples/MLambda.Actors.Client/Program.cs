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

namespace MLambda.Actors.Client
{
    using System;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using MLambda.Actors.Satellite;
    using MLambda.Actors.Satellite.Abstraction;
    using MLambda.Actors.Server;

    /// <summary>
    /// The actor client that connects to a cluster and invokes remote actors.
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
            var address = ActorCatalog.Build(config =>
            {
                config.NodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? "client-1";
                config.Port = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "6000");
                config.SeedNodes = ActorCatalogConfig.ParseSeedNodes(Environment.GetEnvironmentVariable("SEED_NODES"));
                config.Register<GreeterActor>();
                config.Register<CalculatorActor>();
            });

            await address.Start();

            Console.WriteLine("[Client] Waiting for cluster convergence...");
            await Task.Delay(5000);

            var greeter = address.For<GreeterActor>();
            var greeting = await greeter.Send("World");
            Console.WriteLine($"[Client] Received: {greeting}");

            var calculator = address.For<CalculatorActor>();
            var result = await calculator.Send(new CalculateRequest
            {
                A = 42,
                B = 8,
                Operation = "multiply",
            });
            Console.WriteLine($"[Client] Received: {result}");

            await address.Stop();
            Console.WriteLine("[Client] Client stopped.");
        }
    }
}
