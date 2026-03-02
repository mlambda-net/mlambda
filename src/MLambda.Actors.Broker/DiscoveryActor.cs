// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DiscoveryActor.cs" company="MLambda">
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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Broker.Abstraction;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Actor responsible for discovering and tracking remote actor routes
    /// across the cluster. Maintains a local cache of known routes
    /// announced by peer nodes.
    /// </summary>
    [Route("discovery")]
    public class DiscoveryActor : Actor
    {
        private readonly ConcurrentDictionary<Guid, HashSet<string>> nodeRoutes;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryActor"/> class.
        /// </summary>
        public DiscoveryActor()
        {
            this.nodeRoutes = new ConcurrentDictionary<Guid, HashSet<string>>();
        }

        /// <inheritdoc/>
        protected override Behavior Receive(object data)
            => data switch
            {
                AnnounceRoutes msg => Actor.Behavior<Unit, AnnounceRoutes>(this.HandleAnnounce, msg),
                DiscoverRoutes _ => Actor.Behavior<DiscoverRoutesResult>(this.HandleDiscover),
                NodeLeft msg => Actor.Behavior<Unit, NodeLeft>(this.HandleNodeLeft, msg),
                _ => Actor.Ignore,
            };

        private IObservable<Unit> HandleAnnounce(AnnounceRoutes msg)
        {
            var routes = this.nodeRoutes.GetOrAdd(msg.SourceNode.NodeId, _ => new HashSet<string>());
            foreach (var route in msg.Routes)
            {
                routes.Add(route);
            }

            return Actor.Done;
        }

        private IObservable<DiscoverRoutesResult> HandleDiscover()
        {
            var allRoutes = new Dictionary<string, NodeEndpoint>();
            foreach (var kvp in this.nodeRoutes)
            {
                foreach (var route in kvp.Value)
                {
                    if (!allRoutes.ContainsKey(route))
                    {
                        allRoutes[route] = new NodeEndpoint(kvp.Key, string.Empty, 0);
                    }
                }
            }

            return Observable.Return(new DiscoverRoutesResult { Routes = allRoutes });
        }

        private IObservable<Unit> HandleNodeLeft(NodeLeft msg)
        {
            this.nodeRoutes.TryRemove(msg.NodeId, out _);
            return Actor.Done;
        }
    }
}
