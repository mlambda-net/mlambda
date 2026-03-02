// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeadLetterActor.cs" company="MLambda">
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

namespace MLambda.Actors.Guardian
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Abstraction.Core;

    /// <summary>
    /// The dead letter actor handles messages sent to stopped or non-existent actors.
    /// </summary>
    [Route("deadLetters")]
    public class DeadLetterActor : Actor
    {
        private readonly IEventStream eventStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeadLetterActor"/> class.
        /// </summary>
        /// <param name="eventStream">The event stream.</param>
        public DeadLetterActor(IEventStream eventStream)
        {
            this.eventStream = eventStream;
        }

        /// <inheritdoc />
        protected override Behavior Receive(object data)
            => data switch
            {
                DeadLetter letter => Actor.Behavior(this.HandleDeadLetter, letter),
                _ => Actor.Ignore,
            };

        private IObservable<Unit> HandleDeadLetter(DeadLetter letter)
        {
            this.eventStream.Publish(letter);
            return Actor.Done;
        }
    }
}
