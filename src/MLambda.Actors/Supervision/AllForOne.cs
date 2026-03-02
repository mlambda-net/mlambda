// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AllForOne.cs" company="MLambda">
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

namespace MLambda.Actors.Supervision
{
    using System;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Abstraction.Core;
    using MLambda.Actors.Abstraction.Supervision;

    /// <summary>
    /// All for one supervision strategy. When one child fails, the directive
    /// is applied to all sibling children.
    /// </summary>
    public class AllForOne : ISupervisor
    {
        private readonly IDecider decider;

        private readonly IBucket bucket;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllForOne"/> class.
        /// </summary>
        /// <param name="decider">The decider.</param>
        /// <param name="bucket">The bucket.</param>
        public AllForOne(IDecider decider, IBucket bucket)
        {
            this.decider = decider;
            this.bucket = bucket;
        }

        /// <summary>
        /// Applies the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The context function.</returns>
        public Func<IMainContext, Task> Apply(IMessage message) =>

            async context =>
            {
                try
                {
                    message.Response(await context.Actor.Receive(message.Payload)(context));
                }
                catch (Exception exception)
                {
                    this.Handle(exception, context.Process);
                }
            };

        /// <summary>
        /// Handles the exception by applying the directive to all sibling processes.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="process">The failed process.</param>
        public void Handle(Exception exception, IProcess process)
        {
            var directive = this.decider.Decision(exception);

            if (process.Parent != null)
            {
                var siblings = this.bucket.Filter(p => p.Parent != null && p.Parent.Id == process.Parent.Id);
                foreach (var sibling in siblings)
                {
                    ApplyDirective(directive, exception, sibling);
                }
            }
            else
            {
                ApplyDirective(directive, exception, process);
            }
        }

        private static void ApplyDirective(Directive directive, Exception exception, IProcess process)
        {
            switch (directive)
            {
                case Directive.Restart:
                    process.Restart();
                    break;
                case Directive.Resume:
                    process.Resume();
                    break;
                case Directive.Stop:
                    process.Stop();
                    break;
                case Directive.Escalate:
                    process.Escalate(exception);
                    break;
                default:
                    throw new InvalidOperationException(nameof(Directive));
            }
        }
    }
}
