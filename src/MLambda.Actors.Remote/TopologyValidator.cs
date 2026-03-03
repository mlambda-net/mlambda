// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TopologyValidator.cs" company="MLambda">
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

namespace MLambda.Actors.Remote
{
    using System;
    using MLambda.Actors.Remote.Abstraction;

    /// <summary>
    /// Validates topology configuration to catch common misconfigurations early.
    /// </summary>
    public static class TopologyValidator
    {
        /// <summary>
        /// Validates the actor address configuration for topology correctness.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the configuration is invalid.
        /// </exception>
        public static void Validate(ActorAddressConfig config)
        {
            switch (config.NodeType)
            {
                case NodeType.Satellite:
                    if (config.ClusterNodes == null || config.ClusterNodes.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "Satellite node requires at least one cluster node endpoint " +
                            "in the ClusterNodes configuration.");
                    }

                    break;

                case NodeType.Cluster:
                    if (config.ActorTypes.Count > 0)
                    {
                        Console.WriteLine(
                            "[WARN] Cluster-only node has registered actor types. " +
                            "User actors will not run on cluster-only nodes. " +
                            "Use NodeType.Hybrid if this node should also execute actors.");
                    }

                    break;

                case NodeType.Hybrid:
                    // Hybrid is valid in all configurations.
                    break;
            }
        }
    }
}
