// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WatcherActor.cs" company="MLambda">
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

namespace MLambda.Actors.Test.Actors
{
    using System;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Test.Actors.Command;

    /// <summary>
    /// Actor that watches another actor and records when it terminates.
    /// Demonstrates the DeathWatch pattern (Watch/Unwatch/Terminated).
    /// </summary>
    [Route("/watcher")]
    public class WatcherActor : Actor
    {
        private bool terminated;

        private IAddress watchedAddress;

        /// <inheritdoc/>
        protected override Behavior Receive(object data) =>
            data switch
            {
                WatchTarget target => Actor.Behavior(this.HandleWatch, target),
                UnwatchTarget _ => Actor.Behavior(this.HandleUnwatch),
                Terminated t => Actor.Behavior(this.HandleTerminated, t),
                IsTerminated _ => Actor.Behavior<bool>(() => Observable.Return(this.terminated)),
                _ => Actor.Ignore,
            };

        private IObservable<string> HandleWatch(IContext context, WatchTarget target)
        {
            this.watchedAddress = target.Address;
            context.Watch(target.Address);
            return Observable.Return("watching");
        }

        private IObservable<string> HandleUnwatch(IContext context)
        {
            if (this.watchedAddress != null)
            {
                context.Unwatch(this.watchedAddress);
                this.watchedAddress = null;
            }

            return Observable.Return("unwatched");
        }

        private IObservable<string> HandleTerminated(Terminated terminated)
        {
            this.terminated = true;
            return Observable.Return("target terminated");
        }
    }
}
