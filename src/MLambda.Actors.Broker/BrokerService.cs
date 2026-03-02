// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BrokerService.cs" company="MLambda">
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

namespace MLambda.Actors.Broker
{
    using System;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Broker.Abstraction;
    using MLambda.Actors.Gossip.Abstraction;

    /// <summary>
    /// Service that manages the lifecycle of broker system actors
    /// and integrates with the cluster for node membership events.
    /// </summary>
    public class BrokerService : IBrokerService
    {
        private readonly ISystemContext systemContext;
        private readonly IRouterTable routerTable;
        private IDisposable clusterSubscription;
        private IAddress brokerAddress;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokerService"/> class.
        /// </summary>
        /// <param name="systemContext">The system context for spawning system actors.</param>
        /// <param name="routerTable">The shared router table.</param>
        public BrokerService(ISystemContext systemContext, IRouterTable routerTable)
        {
            this.systemContext = systemContext;
            this.routerTable = routerTable;
        }

        /// <inheritdoc/>
        public void Start()
        {
            this.brokerAddress = this.systemContext.Spawn<BrokerActor>().Wait();
        }

        /// <inheritdoc/>
        public void Stop()
        {
            this.clusterSubscription?.Dispose();
            this.brokerAddress?.Dispose();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Stop();
        }
    }
}
