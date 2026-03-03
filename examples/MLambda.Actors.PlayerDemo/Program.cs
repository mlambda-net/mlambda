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

namespace MLambda.Actors.PlayerDemo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Asteroids;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Satellite;
    using MLambda.Actors.Satellite.Abstraction;

    /// <summary>
    /// Demo: 10 player actors con rutas parametrizadas.
    /// Soporta tres modos de ejecucion:
    /// <list type="bullet">
    /// <item>Default: demo all-in-one (Hybrid + Asteroid en un proceso).</item>
    /// <item>--docker: Asteroid client conectando a un cluster Docker Compose.</item>
    /// <item>NODE_TYPE env var: server mode para correr dentro de Docker.</item>
    /// </list>
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
            var nodeTypeEnv = Environment.GetEnvironmentVariable("NODE_TYPE");

            if (!string.IsNullOrWhiteSpace(nodeTypeEnv))
            {
                // SERVER MODE: running inside a Docker container.
                await RunServerMode();
            }
            else if (args.Contains("--docker"))
            {
                // ASTEROID CLIENT MODE: connect to a Docker Compose cluster from host.
                await RunDockerAsteroidMode();
            }
            else
            {
                // ORIGINAL MODE: all-in-one Hybrid + Asteroid demo.
                await RunOriginalDemo();
            }
        }

        /// <summary>
        /// Runs the original all-in-one demo with a Hybrid node and an Asteroid
        /// in the same process. No Docker required.
        /// </summary>
        private static async Task RunOriginalDemo()
        {
            Console.WriteLine("=== Demo: 10 Player Actors con Secreto ===");
            Console.WriteLine();

            // ── 1. Nodo Hybrid (Cluster + Satellite) en puerto 15000 ──
            // NodeId must be a resolvable hostname (used by TcpTransport.ConnectAsync).
            var hybrid = ActorCatalog.Build(config =>
            {
                config.NodeId = "localhost";
                config.Port = 15000;
                config.NodeType = NodeType.Hybrid;
                config.SeedNodes = new List<NodeEndpoint>
                {
                    new NodeEndpoint("localhost", 15000),
                };
                config.Register<PlayerActor>();
            });

            await hybrid.Start();
            Console.WriteLine("[Hybrid] Nodo hybrid iniciado en puerto 15000");

            // ── 2. Nodo Asteroid (gateway liviano) en puerto 16000 ──
            var asteroid = AsteroidCatalog.Build(config =>
            {
                config.NodeId = "localhost";
                config.Port = 16000;
                config.NodeType = NodeType.Asteroid;
                config.ClusterNodes = new List<NodeEndpoint>
                {
                    new NodeEndpoint("localhost", 15000),
                };
            });

            await asteroid.Start();
            Console.WriteLine("[Asteroid] Nodo asteroid conectado al cluster en puerto 16000");

            // ── 3. Esperar convergencia (registro + heartbeat) ──
            Console.WriteLine("[Cluster] Esperando convergencia del cluster (5s)...");
            await Task.Delay(5000);

            // ── 4. Preguntar secretos a los 10 jugadores ──
            await QueryAllPlayers(asteroid);

            // ── 5. Limpiar ──
            await asteroid.Stop();
            await hybrid.Stop();

            Console.WriteLine("[Demo] Nodos detenidos. Fin.");
        }

        /// <summary>
        /// Runs as a server node inside a Docker container.
        /// Reads configuration from environment variables and waits for Ctrl+C.
        /// </summary>
        private static async Task RunServerMode()
        {
            var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? "node-1";
            var nodeType = ActorCatalogConfig.ParseNodeType(
                Environment.GetEnvironmentVariable("NODE_TYPE"));
            var port = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "5000");

            IActorCatalog node;
            if (nodeType == NodeType.Asteroid)
            {
                node = AsteroidCatalog.Build(config =>
                {
                    config.NodeId = nodeId;
                    config.Port = port;
                    config.NodeType = nodeType;
                    config.ClusterNodes = ActorCatalogConfig.ParseClusterNodes(
                        Environment.GetEnvironmentVariable("CLUSTER_NODES"));
                    config.EnableMonitoring = false;
                });
            }
            else
            {
                node = ActorCatalog.Build(config =>
                {
                    config.NodeId = nodeId;
                    config.Port = port;
                    config.NodeType = nodeType;
                    config.SeedNodes = ActorCatalogConfig.ParseSeedNodes(
                        Environment.GetEnvironmentVariable("SEED_NODES"));
                    config.ClusterNodes = ActorCatalogConfig.ParseClusterNodes(
                        Environment.GetEnvironmentVariable("CLUSTER_NODES"));
                    config.EnableMonitoring = false;

                    // Register PlayerActor only for nodes that host user actors.
                    if (nodeType == NodeType.Satellite || nodeType == NodeType.Hybrid)
                    {
                        config.Register<PlayerActor>();
                    }
                });
            }

            Console.WriteLine($"[Node] Starting {nodeType} node '{nodeId}' on port {port}");
            await node.Start();
            Console.WriteLine("[Node] Node is ready. Press Ctrl+C to shut down.");

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
            Console.WriteLine("[Node] Node stopped.");
        }

        /// <summary>
        /// Runs the Asteroid client on the host, connecting to a Docker Compose
        /// cluster via exposed ports. Usage: dotnet run -- --docker
        /// </summary>
        private static async Task RunDockerAsteroidMode()
        {
            Console.WriteLine("=== Demo: PlayerDemo via Docker Compose ===");
            Console.WriteLine();

            // NodeId = "host.docker.internal" so cluster containers can
            // send TCP responses back to the host machine.
            var asteroid = AsteroidCatalog.Build(config =>
            {
                config.NodeId = "host.docker.internal";
                config.Port = 16000;
                config.NodeType = NodeType.Asteroid;
                config.EnableMonitoring = false;
                config.ClusterNodes = new List<NodeEndpoint>
                {
                    new NodeEndpoint("localhost", 5001),
                    new NodeEndpoint("localhost", 5002),
                };
            });

            await asteroid.Start();
            Console.WriteLine("[Asteroid] Connected to Docker cluster on ports 5001, 5002");

            Console.WriteLine("[Asteroid] Waiting for cluster convergence (8s)...");
            await Task.Delay(8000);

            // ── Preguntar secretos a los 10 jugadores ──
            await QueryAllPlayers(asteroid);

            await asteroid.Stop();
        }

        /// <summary>
        /// Queries all 10 player actors for their secrets.
        /// </summary>
        /// <param name="asteroid">The asteroid catalog to route messages through.</param>
        private static async Task QueryAllPlayers(IActorCatalog asteroid)
        {
            Console.WriteLine();
            Console.WriteLine("[Asteroid] Preguntando secretos a 10 jugadores...");
            Console.WriteLine();

            for (int i = 1; i <= 10; i++)
            {
                try
                {
                    var player = asteroid.For<PlayerActor>(new Parameter { ["id"] = i });
                    var secret = await player.Send<GetSecret, string>(new GetSecret { Id = i })
                        .Timeout(TimeSpan.FromSeconds(10))
                        .FirstAsync();
                    Console.WriteLine($"  [Asteroid] Player {i} dice: {secret}");
                }
                catch (TimeoutException)
                {
                    Console.WriteLine($"  [Asteroid] Player {i}: TIMEOUT (10s)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  [Asteroid] Player {i}: ERROR - {ex.GetType().Name}: {ex.Message}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("[Asteroid] Demo completo!");
        }
    }
}
