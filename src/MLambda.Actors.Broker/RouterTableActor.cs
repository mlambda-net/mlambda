// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RouterTableActor.cs" company="MLambda">
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
    using MLambda.Actors.Broker.Abstraction;

    /// <summary>
    /// Actor that manages the router table, processing route registration,
    /// lookup, and removal messages in a single-threaded actor context.
    /// </summary>
    [Route("routerTable")]
    public class RouterTableActor : Actor
    {
        private readonly IRouterTable routerTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="RouterTableActor"/> class.
        /// </summary>
        /// <param name="routerTable">The shared router table.</param>
        public RouterTableActor(IRouterTable routerTable)
        {
            this.routerTable = routerTable;
        }

        /// <inheritdoc/>
        protected override Behavior Receive(object data)
            => data switch
            {
                RegisterRoute msg => Actor.Behavior<Unit, RegisterRoute>(this.HandleRegister, msg),
                UnregisterRoute msg => Actor.Behavior<Unit, UnregisterRoute>(this.HandleUnregister, msg),
                LookupRoute msg => Actor.Behavior<LookupRouteResult, LookupRoute>(this.HandleLookup, msg),
                DiscoverRoutes _ => Actor.Behavior<DiscoverRoutesResult>(this.HandleDiscover),
                NodeLeft msg => Actor.Behavior<Unit, NodeLeft>(this.HandleNodeLeft, msg),
                _ => Actor.Ignore,
            };

        private IObservable<Unit> HandleRegister(RegisterRoute msg)
        {
            this.routerTable.AddRoute(msg.Route, null);
            return Actor.Done;
        }

        private IObservable<Unit> HandleUnregister(UnregisterRoute msg)
        {
            this.routerTable.RemoveRoute(msg.Route);
            return Actor.Done;
        }

        private IObservable<LookupRouteResult> HandleLookup(LookupRoute msg)
        {
            var node = this.routerTable.LookupRoute(msg.Route);
            return Observable.Return(new LookupRouteResult { Route = msg.Route, Node = node });
        }

        private IObservable<DiscoverRoutesResult> HandleDiscover()
        {
            return Observable.Return(new DiscoverRoutesResult { Routes = this.routerTable.GetAll() });
        }

        private IObservable<Unit> HandleNodeLeft(NodeLeft msg)
        {
            this.routerTable.RemoveNode(msg.NodeId);
            return Actor.Done;
        }
    }
}
