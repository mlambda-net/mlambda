// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CounterActor.cs" company="MLambda">
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
    using MLambda.Actors.Test.Actors.Command;

    /// <summary>
    /// Simple stateful actor that maintains a counter.
    /// Used for testing state persistence across messages
    /// and state reset on restart.
    /// </summary>
    [Route("/counter")]
    public class CounterActor : Actor
    {
        private int count;

        /// <inheritdoc/>
        protected override Behavior Receive(object data) =>
            data switch
            {
                Increment _ => Actor.Behavior<int>(this.HandleIncrement),
                Decrement _ => Actor.Behavior<int>(this.HandleDecrement),
                GetCount _ => Actor.Behavior<int>(() => Observable.Return(this.count)),
                _ => Actor.Ignore,
            };

        private IObservable<int> HandleIncrement()
        {
            this.count++;
            return Observable.Return(this.count);
        }

        private IObservable<int> HandleDecrement()
        {
            this.count--;
            return Observable.Return(this.count);
        }
    }
}
