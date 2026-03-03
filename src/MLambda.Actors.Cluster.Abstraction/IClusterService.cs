// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IClusterService.cs" company="MLambda">
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

namespace MLambda.Actors.Cluster.Abstraction
{
    using System;

    /// <summary>
    /// Lifecycle interface for the cluster service.
    /// Manages RouteActor, StateActor, and DeliveryActor on cluster nodes.
    /// </summary>
    public interface IClusterService : IDisposable
    {
        /// <summary>
        /// Starts the cluster service, spawning all system actors
        /// and beginning CRDT replication.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the cluster service, disposing actors and replication
        /// in reverse order.
        /// </summary>
        void Stop();
    }
}
