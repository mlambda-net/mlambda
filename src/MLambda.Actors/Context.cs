// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Context.cs" company="MLambda">
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

namespace MLambda.Actors
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Abstraction.Core;

    /// <summary>
    /// The actor context task.
    /// </summary>
    public class Context : IMainContext
    {
        private readonly IBucket bucket;

        /// <summary>
        /// Initializes a new instance of the <see cref="Context"/> class.
        /// </summary>
        /// <param name="process">the process.</param>
        /// <param name="bucket">the bucket.</param>
        public Context(IProcess process, IBucket bucket)
        {
            this.Process = process;
            this.bucket = bucket;
        }

        /// <summary>
        /// Gets the current actor.
        /// </summary>
        public IActor Actor => this.Process.Current.Actor;

        /// <summary>
        /// Gets the process.
        /// </summary>
        public IProcess Process { get; }

        /// <summary>
        /// Gets the self address.
        /// </summary>
        public IAddress Self => this.Process.Current.Address;

        /// <summary>
        /// Spawns a new actor.
        /// </summary>
        /// <typeparam name="T">the type of the actor.</typeparam>
        /// <returns>The address.</returns>
        public IObservable<IAddress> Spawn<T>()
            where T : IActor
        {
            return Observable.Return(this.Process.Spawn<T>());
        }

        /// <summary>
        /// Spawns a new actor by runtime type.
        /// </summary>
        /// <param name="actorType">The CLR type of the actor to spawn.</param>
        /// <returns>The address.</returns>
        public IObservable<IAddress> Spawn(Type actorType)
        {
            return Observable.Return(this.Process.Spawn(actorType));
        }

        /// <summary>
        /// Watches an actor for termination. Registers self to receive
        /// a Terminated message when the target actor stops.
        /// </summary>
        /// <param name="address">The address of the actor to watch.</param>
        public void Watch(IAddress address)
        {
            var target = this.bucket.Filter(p => p.Current.Address.Id == address.Id).FirstOrDefault();
            target?.Watch(this.Self);
        }

        /// <summary>
        /// Stops watching an actor for termination.
        /// </summary>
        /// <param name="address">The address of the actor to unwatch.</param>
        public void Unwatch(IAddress address)
        {
            var target = this.bucket.Filter(p => p.Current.Address.Id == address.Id).FirstOrDefault();
            target?.Unwatch(this.Self);
        }
    }
}
