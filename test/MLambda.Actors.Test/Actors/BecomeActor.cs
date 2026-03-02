// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BecomeActor.cs" company="MLambda">
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
    /// Actor that demonstrates Become/Unbecome behavior switching.
    /// Starts in "normal" mode, can switch to "angry" or "happy" mode,
    /// and can revert back with Unbecome.
    /// </summary>
    [Route("/become")]
    public class BecomeActor : Actor
    {
        /// <inheritdoc/>
        protected override Behavior Receive(object data) =>
            data switch
            {
                SetMood mood when mood.Mood == "happy" => Actor.Behavior(this.BecomeHappy),
                SetMood mood when mood.Mood == "angry" => Actor.Behavior(this.BecomeAngry),
                SetMood mood when mood.Mood == "normal" => Actor.Behavior(this.BecomeNormal),
                AskMood _ => Actor.Behavior<string>(ctx => Observable.Return("normal")),
                _ => Actor.Ignore,
            };

        private IObservable<string> BecomeHappy(IContext context)
        {
            this.Become(this.HappyBehavior);
            return Observable.Return("switched to happy");
        }

        private IObservable<string> BecomeAngry(IContext context)
        {
            this.Become(this.AngryBehavior);
            return Observable.Return("switched to angry");
        }

        private IObservable<string> BecomeNormal(IContext context)
        {
            this.Unbecome();
            return Observable.Return("switched to normal");
        }

        private Behavior HappyBehavior(object data) =>
            data switch
            {
                AskMood _ => Actor.Behavior<string>(() => Observable.Return("happy")),
                SetMood mood when mood.Mood == "normal" => Actor.Behavior(this.BecomeNormal),
                SetMood mood when mood.Mood == "angry" => Actor.Behavior(this.BecomeAngry),
                _ => Actor.Ignore,
            };

        private Behavior AngryBehavior(object data) =>
            data switch
            {
                AskMood _ => Actor.Behavior<string>(() => Observable.Return("angry")),
                SetMood mood when mood.Mood == "normal" => Actor.Behavior(this.BecomeNormal),
                SetMood mood when mood.Mood == "happy" => Actor.Behavior(this.BecomeHappy),
                _ => Actor.Ignore,
            };
    }
}
