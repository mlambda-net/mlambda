// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ITransport.cs" company="MLambda">
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

namespace MLambda.Actors.Network.Abstraction
{
    using System;
    using System.Reactive;

    /// <summary>
    /// Transport layer for sending and receiving envelopes across network nodes.
    /// </summary>
    public interface ITransport : IDisposable
    {
        /// <summary>
        /// Gets the local endpoint for this transport.
        /// </summary>
        NodeEndpoint LocalEndpoint { get; }

        /// <summary>
        /// Gets the stream of incoming envelopes.
        /// </summary>
        IObservable<Envelope> IncomingMessages { get; }

        /// <summary>
        /// Sends an envelope to a target node.
        /// </summary>
        /// <param name="target">The target endpoint.</param>
        /// <param name="envelope">The envelope to send.</param>
        /// <returns>An observable that completes when the send is done.</returns>
        IObservable<Unit> Send(NodeEndpoint target, Envelope envelope);

        /// <summary>
        /// Starts the transport listener.
        /// </summary>
        /// <returns>An observable that completes when started.</returns>
        IObservable<Unit> Start();

        /// <summary>
        /// Stops the transport listener.
        /// </summary>
        /// <returns>An observable that completes when stopped.</returns>
        IObservable<Unit> Stop();
    }
}
