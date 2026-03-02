// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LifecycleActor.cs" company="MLambda">
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
    using MLambda.Actors.Test.Actors.Command;

    /// <summary>
    /// Actor that tracks lifecycle hook calls for verification.
    /// Records PreStart, PostStop, PreRestart, PostRestart events.
    /// </summary>
    [Route("/lifecycle")]
    public class LifecycleActor : Actor
    {
        /// <summary>
        /// Static log shared across instances to survive restarts.
        /// </summary>
        public static List<string> LifecycleLog { get; } = new List<string>();

        /// <inheritdoc/>
        public override void PreStart()
        {
            LifecycleLog.Add("PreStart");
        }

        /// <inheritdoc/>
        public override void PostStop()
        {
            LifecycleLog.Add("PostStop");
        }

        /// <inheritdoc/>
        public override void PreRestart(Exception reason)
        {
            LifecycleLog.Add("PreRestart");
        }

        /// <inheritdoc/>
        public override void PostRestart(Exception reason)
        {
            LifecycleLog.Add("PostRestart");
        }

        /// <inheritdoc/>
        protected override Behavior Receive(object data) =>
            data switch
            {
                GetLifecycleLog _ => Actor.Behavior<List<string>>(() => Observable.Return(new List<string>(LifecycleLog))),
                string msg when msg == "ping" => Actor.Behavior<string>(() => Observable.Return("pong")),
                _ => Actor.Ignore,
            };
    }
}
