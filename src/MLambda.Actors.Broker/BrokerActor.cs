// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BrokerActor.cs" company="MLambda">
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
    using System.Reactive;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Broker.Abstraction;

    /// <summary>
    /// Top-level broker actor that orchestrates discovery and routing.
    /// Spawns DiscoveryActor and RouterTableActor as children.
    /// </summary>
    [Route("broker")]
    public class BrokerActor : Actor
    {
        private readonly IRouterTable routerTable;
        private IAddress discoveryAddress;
        private IAddress routerTableAddress;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokerActor"/> class.
        /// </summary>
        /// <param name="routerTable">The shared router table.</param>
        public BrokerActor(IRouterTable routerTable)
        {
            this.routerTable = routerTable;
        }

        /// <inheritdoc/>
        protected override Behavior Receive(object data)
            => data switch
            {
                RegisterRoute msg => Actor.Behavior<Unit, RegisterRoute>(this.HandleRegisterRoute, msg),
                UnregisterRoute msg => Actor.Behavior<Unit, UnregisterRoute>(this.HandleUnregisterRoute, msg),
                LookupRoute msg => Actor.Behavior<LookupRouteResult, LookupRoute>(this.HandleLookupRoute, msg),
                DiscoverRoutes msg => Actor.Behavior<DiscoverRoutesResult>(this.HandleDiscoverRoutes),
                NodeLeft msg => Actor.Behavior<Unit, NodeLeft>(this.HandleNodeLeft, msg),
                AnnounceRoutes msg => Actor.Behavior<Unit, AnnounceRoutes>(this.HandleAnnounceRoutes, msg),
                _ => Actor.Ignore,
            };

        private IObservable<Unit> HandleRegisterRoute(RegisterRoute msg)
        {
            this.routerTable.AddRoute(msg.Route, null);
            return Actor.Done;
        }

        private IObservable<Unit> HandleUnregisterRoute(UnregisterRoute msg)
        {
            this.routerTable.RemoveRoute(msg.Route);
            return Actor.Done;
        }

        private IObservable<LookupRouteResult> HandleLookupRoute(LookupRoute msg)
        {
            var node = this.routerTable.LookupRoute(msg.Route);
            return Observable.Return(new LookupRouteResult { Route = msg.Route, Node = node });
        }

        private IObservable<DiscoverRoutesResult> HandleDiscoverRoutes()
        {
            return Observable.Return(new DiscoverRoutesResult { Routes = this.routerTable.GetAll() });
        }

        private IObservable<Unit> HandleNodeLeft(NodeLeft msg)
        {
            this.routerTable.RemoveNode(msg.NodeId);
            return Actor.Done;
        }

        private IObservable<Unit> HandleAnnounceRoutes(AnnounceRoutes msg)
        {
            foreach (var route in msg.Routes)
            {
                this.routerTable.AddRoute(route, msg.SourceNode);
            }

            return Actor.Done;
        }
    }
}
