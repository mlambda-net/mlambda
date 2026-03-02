// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Actor.cs" company="MLambda">
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

namespace MLambda.Actors.Abstraction
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Observable.Aliases;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Abstraction.Supervision;

    /// <summary>
    /// The actor implementation.
    /// </summary>
    public abstract class Actor : IActor
    {
        /// <summary>
        /// The current receive handler override set by Become.
        /// </summary>
        private Func<object, Behavior> currentReceiveHandler;

        /// <summary>
        /// Gets The default value.
        /// </summary>
        public static IObservable<object> Default => Observable.Return((object)Unit.Default);

        /// <summary>
        /// Gets the done value.
        /// </summary>
        public static IObservable<Unit> Done => Observable.Return(Unit.Default);

        /// <summary>
        /// Gets The ignore behavior.
        /// </summary>
        public static Behavior Ignore => _ => Default;

        /// <summary>
        /// Gets or sets the supervisor strategy.
        /// </summary>
        public virtual ISupervisor Supervisor { get; protected set; }

        /// <summary>
        /// Gets or sets the stash for storing messages temporarily.
        /// </summary>
        public IStash Stash { get; set; }

        /// <summary>
        /// The Behavior handler for the message.
        /// </summary>
        /// <param name="apply">the lambda method.</param>
        /// <typeparam name="To">The type of the response.</typeparam>
        /// <returns>The behavior.</returns>
        public static Behavior Behavior<To>(Func<IContext, IObservable<To>> apply) =>
            ctx => apply(ctx).Map(val => (object)val);

        /// <summary>
        /// The Behavior handler for the message.
        /// </summary>
        /// <param name="apply">the lambda method.</param>
        /// <param name="a">the value a.</param>
        /// <typeparam name="To">The type of the response.</typeparam>
        /// <typeparam name="Ta">The type of a.</typeparam>
        /// <returns>The behavior.</returns>
        public static Behavior Behavior<To, Ta>(Func<IContext, Ta, IObservable<To>> apply, Ta a) =>
            ctx => apply(ctx, a).Map(val => (object)val);

        /// <summary>
        /// The Behavior handler for the message.
        /// </summary>
        /// <param name="apply">the lambda method.</param>
        /// <param name="a">the value a.</param>
        /// <param name="b">the value b.</param>
        /// <typeparam name="To">The type of the response.</typeparam>
        /// <typeparam name="Ta">The type of a.</typeparam>
        /// <typeparam name="Tb">The type of b.</typeparam>
        /// <returns>The behavior.</returns>
        public static Behavior Behavior<To, Ta, Tb>(Func<IContext, Ta, Tb, IObservable<To>> apply, Ta a, Tb b) =>
            ctx => apply(ctx, a, b).Map(val => (object)val);

        /// <summary>
        /// The Behavior handler for the message.
        /// </summary>
        /// <param name="apply">the lambda method.</param>
        /// <param name="a">the value a.</param>
        /// <param name="b">the value b.</param>
        /// <param name="c">the value c.</param>
        /// <typeparam name="To">The type of the response.</typeparam>
        /// <typeparam name="Ta">The type of a.</typeparam>
        /// <typeparam name="Tb">The type of b.</typeparam>
        /// <typeparam name="Tc">The type of c.</typeparam>
        /// <returns>The behavior.</returns>
        public static Behavior Behavior<To, Ta, Tb, Tc>(Func<IContext, Ta, Tb, Tc, IObservable<To>> apply, Ta a, Tb b, Tc c) =>
            ctx => apply(ctx, a, b, c).Map(val => (object)val);

        /// <summary>
        /// The Behavior handler for the message.
        /// </summary>
        /// <param name="apply">the lambda method.</param>
        /// <typeparam name="To">The type of the response.</typeparam>
        /// <returns>The behavior.</returns>
        public static Behavior Behavior<To>(Func<IObservable<To>> apply) => _ => apply().Map(val => (object)val);

        /// <summary>
        /// The Behavior handler for the message.
        /// </summary>
        /// <param name="apply">the lambda method.</param>
        /// <param name="a">the value a.</param>
        /// <typeparam name="To">The type of the response.</typeparam>
        /// <typeparam name="Ta">The type of a.</typeparam>
        /// <returns>The behavior.</returns>
        public static Behavior Behavior<To, Ta>(Func<Ta, IObservable<To>> apply, Ta a) =>
            _ => apply(a).Map(val => (object)val);

        /// <summary>
        /// The Behavior handler for the message.
        /// </summary>
        /// <param name="apply">the lambda method.</param>
        /// <param name="a">the value a.</param>
        /// <param name="b">the value b.</param>
        /// <typeparam name="To">The type of the response.</typeparam>
        /// <typeparam name="Ta">The type of a.</typeparam>
        /// <typeparam name="Tb">The type of b.</typeparam>
        /// <returns>The behavior.</returns>
        public static Behavior Behavior<To, Ta, Tb>(Func<Ta, Tb, IObservable<To>> apply, Ta a, Tb b) =>
            _ => apply(a, b).Map(val => (object)val);

        /// <summary>
        /// The Behavior handler for the message.
        /// </summary>
        /// <param name="apply">the lambda method.</param>
        /// <param name="a">the value a.</param>
        /// <param name="b">the value b.</param>
        /// <param name="c">the value c.</param>
        /// <typeparam name="To">The type of the response.</typeparam>
        /// <typeparam name="Ta">The type of a.</typeparam>
        /// <typeparam name="Tb">The type of b.</typeparam>
        /// <typeparam name="Tc">The type of c.</typeparam>
        /// <returns>The behavior.</returns>
        public static Behavior Behavior<To, Ta, Tb, Tc>(Func<Ta, Tb, Tc, IObservable<To>> apply, Ta a, Tb b, Tc c) =>
            _ => apply(a, b, c).Map(val => (object)val);

        /// <summary>
        /// The Behavior handler for the message.
        /// </summary>
        /// <param name="apply">the lambda method.</param>
        /// <param name="a">the value a.</param>
        /// <param name="b">the value b.</param>
        /// <param name="c">the value c.</param>
        /// <param name="d">the value d.</param>
        /// <typeparam name="To">The type of the response.</typeparam>
        /// <typeparam name="Ta">The type of a.</typeparam>
        /// <typeparam name="Tb">The type of b.</typeparam>
        /// <typeparam name="Tc">The type of c.</typeparam>
        /// <typeparam name="Td">The type of d.</typeparam>
        /// <returns>The behavior.</returns>
        public static Behavior Behavior<To, Ta, Tb, Tc, Td>(Func<Ta, Tb, Tc, Td, IObservable<To>> apply, Ta a, Tb b, Tc c, Td d) =>
            _ => apply(a, b, c, d).Map(val => (object)val);

        /// <summary>
        /// The Behavior handler for the message.
        /// </summary>
        /// <param name="apply">the lambda method.</param>
        /// <param name="a">the value a.</param>
        /// <param name="b">the value b.</param>
        /// <param name="c">the value c.</param>
        /// <param name="d">the value d.</param>
        /// <typeparam name="To">The type of the response.</typeparam>
        /// <typeparam name="Ta">The type of a.</typeparam>
        /// <typeparam name="Tb">The type of b.</typeparam>
        /// <typeparam name="Tc">The type of c.</typeparam>
        /// <typeparam name="Td">The type of d.</typeparam>
        /// <returns>The behavior.</returns>
        public static Behavior Behavior<To, Ta, Tb, Tc, Td>(Func<IContext, Ta, Tb, Tc, Td, IObservable<To>> apply, Ta a, Tb b, Tc c, Td d) =>
            ctx => apply(ctx, a, b, c, d).Map(val => (object)val);

        /// <summary>
        /// Switches the actor behavior to a new receive handler.
        /// The new handler will be used to process all subsequent messages
        /// until <see cref="Unbecome"/> is called.
        /// </summary>
        /// <param name="receiveHandler">The new receive handler function.</param>
        public void Become(Func<object, Behavior> receiveHandler) => this.currentReceiveHandler = receiveHandler;

        /// <summary>
        /// Reverts the actor behavior to the default receive handler.
        /// </summary>
        public void Unbecome() => this.currentReceiveHandler = null;

        /// <summary>
        /// Unstashes the most recently stashed message.
        /// </summary>
        public void Unstash() => this.Stash?.Unstash();

        /// <summary>
        /// Unstashes all stashed messages.
        /// </summary>
        public void UnstashAll() => this.Stash?.UnstashAll();

        /// <summary>
        /// Called before the actor starts processing messages.
        /// </summary>
        public virtual void PreStart()
        {
        }

        /// <summary>
        /// Called after the actor has been stopped.
        /// </summary>
        public virtual void PostStop()
        {
        }

        /// <summary>
        /// Called before the actor is restarted due to an exception.
        /// </summary>
        /// <param name="reason">The exception that caused the restart.</param>
        public virtual void PreRestart(Exception reason)
        {
        }

        /// <summary>
        /// Called after the actor has been restarted due to an exception.
        /// </summary>
        /// <param name="reason">The exception that caused the restart.</param>
        public virtual void PostRestart(Exception reason)
        {
        }

        /// <summary>
        /// Receives the message.
        /// </summary>
        /// <param name="data">the data.</param>
        /// <returns>The match rules.</returns>
        Behavior IActor.Receive(object data) => this.currentReceiveHandler != null
            ? this.currentReceiveHandler(data)
            : this.Receive(data);

        /// <summary>
        /// Receives the message.
        /// </summary>
        /// <param name="data">the data.</param>
        /// <returns>The match rules.</returns>
        protected abstract Behavior Receive(object data);
    }
}