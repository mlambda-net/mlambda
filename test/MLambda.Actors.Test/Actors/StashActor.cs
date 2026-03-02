// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StashActor.cs" company="MLambda">
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
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Test.Actors.Command;

    /// <summary>
    /// Actor that demonstrates Stash with Become.
    /// Starts in "initializing" state where it stashes all messages
    /// except the Initialize command, then switches to "ready" state
    /// and unstashes all pending messages.
    /// </summary>
    [Route("/stash")]
    public class StashActor : Actor
    {
        private readonly List<string> processed = new List<string>();

        /// <inheritdoc/>
        protected override Behavior Receive(object data) =>
            data switch
            {
                Initialize _ => Actor.Behavior(this.HandleInitialize),
                GetProcessed _ => Actor.Behavior<List<string>>(() => Observable.Return(new List<string>(this.processed))),
                string _ => Actor.Behavior(this.StashMessage),
                _ => Actor.Ignore,
            };

        private IObservable<string> HandleInitialize(IContext context)
        {
            this.Become(this.ReadyBehavior);
            this.UnstashAll();
            return Observable.Return("initialized");
        }

        private IObservable<string> StashMessage(IContext context)
        {
            this.Stash?.Stash();
            return Observable.Return("stashed");
        }

        private Behavior ReadyBehavior(object data) =>
            data switch
            {
                string message => Actor.Behavior(this.ProcessMessage, message),
                GetProcessed _ => Actor.Behavior<List<string>>(() => Observable.Return(new List<string>(this.processed))),
                _ => Actor.Ignore,
            };

        private IObservable<string> ProcessMessage(string message)
        {
            this.processed.Add(message);
            return Observable.Return($"processed: {message}");
        }
    }
}
