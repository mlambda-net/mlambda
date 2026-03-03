// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayerActor.cs" company="MLambda">
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

namespace MLambda.Actors.PlayerDemo
{
    using System;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;

    /// <summary>
    /// A player actor with a parameterized route. Each player ID
    /// resolves to its own actor instance (e.g. player/1, player/2, ...).
    /// When asked for its secret, it responds with "mi secreto es el numero {id}".
    /// </summary>
    [Route("player/{id}")]
    public class PlayerActor : Actor
    {
        /// <inheritdoc/>
        protected override Behavior Receive(object data) =>
            data switch
            {
                GetSecret msg => Actor.Behavior(this.RevealSecret, msg),
                _ => Actor.Ignore,
            };

        private IObservable<string> RevealSecret(GetSecret msg)
        {
            var secret = $"mi secreto es el numero {msg.Id}";
            Console.WriteLine($"  [Player {msg.Id}] Revelando: {secret}");
            return Observable.Return(secret);
        }
    }
}
