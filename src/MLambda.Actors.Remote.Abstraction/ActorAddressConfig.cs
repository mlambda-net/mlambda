// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActorAddressConfig.cs" company="MLambda">
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

namespace MLambda.Actors.Remote.Abstraction
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Configuration for a remote actor node.
    /// </summary>
    public class ActorAddressConfig
    {
        private readonly List<Type> actorTypes = new List<Type>();

        /// <summary>
        /// Gets or sets the unique node identifier.
        /// </summary>
        public string NodeId { get; set; } = "node-1";

        /// <summary>
        /// Gets or sets the port to listen on.
        /// </summary>
        public int Port { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the node type in the cluster topology.
        /// Defaults to <see cref="Abstraction.NodeType.Hybrid"/> for backward compatibility.
        /// </summary>
        public NodeType NodeType { get; set; } = NodeType.Hybrid;

        /// <summary>
        /// Gets or sets the seed nodes for gossip cluster membership.
        /// Used by Cluster and Hybrid nodes for the gossip mesh.
        /// </summary>
        public List<NodeEndpoint> SeedNodes { get; set; } = new List<NodeEndpoint>();

        /// <summary>
        /// Gets or sets the cluster node endpoints that a Satellite connects to.
        /// Used by Satellite nodes to register with cluster coordinators.
        /// </summary>
        public List<NodeEndpoint> ClusterNodes { get; set; } = new List<NodeEndpoint>();

        /// <summary>
        /// Gets or sets the gossip interval.
        /// </summary>
        public TimeSpan GossipInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the heartbeat interval.
        /// </summary>
        public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets or sets the phi accrual failure detector threshold.
        /// </summary>
        public double PhiThreshold { get; set; } = 8.0;

        /// <summary>
        /// Gets or sets the suspect timeout.
        /// </summary>
        public TimeSpan SuspectTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets a value indicating whether monitoring is enabled.
        /// </summary>
        public bool EnableMonitoring { get; set; } = true;

        /// <summary>
        /// Gets or sets the OpenTelemetry OTLP exporter endpoint for distributed tracing.
        /// </summary>
        public string OtlpEndpoint { get; set; } = "http://localhost:4317";

        /// <summary>
        /// Gets the registered actor types.
        /// </summary>
        public IReadOnlyList<Type> ActorTypes => this.actorTypes;

        /// <summary>
        /// Gets the local endpoint derived from NodeId, Host, and Port.
        /// </summary>
        public NodeEndpoint LocalEndpoint => new NodeEndpoint(this.NodeId, this.Port);

        /// <summary>
        /// Registers an actor type to be available in the node.
        /// </summary>
        /// <typeparam name="T">The actor type.</typeparam>
        /// <returns>This config instance for chaining.</returns>
        public ActorAddressConfig Register<T>()
            where T : class, IActor
        {
            this.actorTypes.Add(typeof(T));
            return this;
        }

        /// <summary>
        /// Parses a semicolon-delimited seed nodes string into a list of endpoints.
        /// Format: "name,port;name,port".
        /// </summary>
        /// <param name="seedNodesEnv">The seed nodes environment variable value.</param>
        /// <returns>A list of parsed <see cref="NodeEndpoint"/> entries.</returns>
        public static List<NodeEndpoint> ParseSeedNodes(string seedNodesEnv)
        {
            return ParseEndpointList(seedNodesEnv);
        }

        /// <summary>
        /// Parses a semicolon-delimited cluster nodes string into a list of endpoints.
        /// Format: "name,port;name,port". Same format as seed nodes.
        /// </summary>
        /// <param name="clusterNodesEnv">The cluster nodes environment variable value.</param>
        /// <returns>A list of parsed <see cref="NodeEndpoint"/> entries.</returns>
        public static List<NodeEndpoint> ParseClusterNodes(string clusterNodesEnv)
        {
            return ParseEndpointList(clusterNodesEnv);
        }

        /// <summary>
        /// Parses a node type string into a <see cref="NodeType"/> enum value.
        /// </summary>
        /// <param name="nodeTypeEnv">The node type environment variable value.</param>
        /// <returns>The parsed <see cref="NodeType"/>. Defaults to <see cref="Abstraction.NodeType.Hybrid"/>.</returns>
        public static NodeType ParseNodeType(string nodeTypeEnv)
        {
            if (string.IsNullOrWhiteSpace(nodeTypeEnv))
            {
                return NodeType.Hybrid;
            }

            return nodeTypeEnv.Trim().ToLowerInvariant() switch
            {
                "cluster" => NodeType.Cluster,
                "satellite" => NodeType.Satellite,
                "hybrid" => NodeType.Hybrid,
                _ => NodeType.Hybrid,
            };
        }

        private static List<NodeEndpoint> ParseEndpointList(string endpointsEnv)
        {
            if (string.IsNullOrWhiteSpace(endpointsEnv))
            {
                return new List<NodeEndpoint>();
            }

            return endpointsEnv
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(entry =>
                {
                    var parts = entry.Split(',');
                    return new NodeEndpoint(
                        parts[0].Trim(),
                        int.Parse(parts[1].Trim()));
                })
                .ToList();
        }
    }
}
