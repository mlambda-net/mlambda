// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GreeterActor.cs" company="MLambda">
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

namespace MLambda.Actors.Server
{
    using System;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;

    /// <summary>
    /// A simple greeter actor that responds to string messages with a greeting.
    /// </summary>
    [Route("greeter")]
    public class GreeterActor : Actor
    {
        /// <inheritdoc/>
        protected override Behavior Receive(object data) =>
            data switch
            {
                string name => Actor.Behavior(this.Greet, name),
                _ => Actor.Ignore,
            };

        private IObservable<string> Greet(string name)
        {
            var greeting = $"Hello, {name}! Greetings from the actor cluster.";
            Console.WriteLine($"[Greeter] {greeting}");
            return Observable.Return(greeting);
        }
    }
}
