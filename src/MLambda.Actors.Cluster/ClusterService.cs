// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClusterService.cs" company="MLambda">
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

namespace MLambda.Actors.Cluster
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Cluster.Abstraction;
    using MLambda.Actors.Gossip.Data;

    /// <summary>
    /// Lifecycle manager for cluster-side system actors.
    /// Starts the CRDT replicator and spawns RouteActor, StateActor, and DeliveryActor.
    /// </summary>
    public class ClusterService : IClusterService
    {
        private readonly ISystemContext systemContext;
        private readonly GossipDataReplicator replicator;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterService"/> class.
        /// </summary>
        /// <param name="systemContext">The system context for spawning actors.</param>
        /// <param name="replicator">The CRDT gossip replicator.</param>
        public ClusterService(ISystemContext systemContext, GossipDataReplicator replicator)
        {
            this.systemContext = systemContext;
            this.replicator = replicator;
            this.SystemActors = new Dictionary<string, IAddress>();
        }

        /// <summary>
        /// Gets the system actor addresses spawned during <see cref="Start"/>.
        /// Keyed by route name (e.g. "route", "state", "delivery").
        /// </summary>
        public IDictionary<string, IAddress> SystemActors { get; }

        /// <inheritdoc/>
        public void Start()
        {
            this.replicator.Start();

            this.SystemActors["route"] = this.systemContext.Spawn<RouteActor>().Wait();
            this.SystemActors["state"] = this.systemContext.Spawn<StateActor>().Wait();
            this.SystemActors["delivery"] = this.systemContext.Spawn<DeliveryActor>().Wait();
        }

        /// <inheritdoc/>
        public void Stop()
        {
            this.replicator.Stop();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes managed resources.
        /// </summary>
        /// <param name="disposing">True if disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed && disposing)
            {
                this.disposed = true;
                this.Stop();
            }
        }
    }
}
