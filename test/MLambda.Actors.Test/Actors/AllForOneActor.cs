// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AllForOneActor.cs" company="MLambda">
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
    using System.Reactive;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Abstraction.Core;
    using MLambda.Actors.Abstraction.Supervision;
    using MLambda.Actors.Supervision;
    using MLambda.Actors.Test.Actors.Command;

    /// <summary>
    /// Actor with AllForOne supervision strategy.
    /// When one child fails, the directive applies to all siblings.
    /// </summary>
    [Route("/allforone")]
    public class AllForOneActor : Actor
    {
        private readonly IBucket bucket;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllForOneActor"/> class.
        /// </summary>
        /// <param name="bucket">The bucket for AllForOne strategy.</param>
        public AllForOneActor(IBucket bucket)
        {
            this.bucket = bucket;
        }

        /// <summary>
        /// Gets the AllForOne supervisor strategy.
        /// </summary>
        public override ISupervisor Supervisor => Strategy.AllForOne(
            decider => decider
                .When<InvalidOperationException>(Directive.Resume)
                .When<InvalidCastException>(Directive.Restart)
                .When<InsufficientMemoryException>(Directive.Stop)
                .Default(Directive.Escalate),
            this.bucket);

        /// <inheritdoc/>
        protected override Behavior Receive(object data) =>
            data switch
            {
                Message message when message.Action == "Resume" => Actor.Behavior(this.DoResume),
                Message message when message.Action == "Restart" => Actor.Behavior(this.DoRestart),
                Message message when message.Action == "Stop" => Actor.Behavior(this.DoStop),
                Message message when message.Action == "Escalate" => Actor.Behavior(this.DoEscalate),
                string _ => Actor.Ignore,
                _ => Actor.Ignore,
            };

        private IObservable<Unit> DoResume()
        {
            throw new InvalidOperationException("resume trigger");
        }

        private IObservable<Unit> DoRestart()
        {
            throw new InvalidCastException("restart trigger");
        }

        private IObservable<Unit> DoStop()
        {
            throw new InsufficientMemoryException("stop trigger");
        }

        private IObservable<Unit> DoEscalate()
        {
            throw new Exception("escalate trigger");
        }
    }
}
