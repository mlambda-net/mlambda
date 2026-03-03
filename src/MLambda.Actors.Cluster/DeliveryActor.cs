// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeliveryActor.cs" company="MLambda">
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

namespace MLambda.Actors.Cluster
{
    using System;
    using System.Collections.Generic;
    using System.Reactive;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Cluster.Abstraction;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Cluster-side system actor that coordinates message delivery to satellite WorkerActors.
    /// Uses a local (non-replicated) dictionary for tracking in-flight deliveries.
    /// </summary>
    [Route("delivery")]
    public class DeliveryActor : Actor
    {
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;
        private readonly Dictionary<Guid, InFlightDelivery> inFlight;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryActor"/> class.
        /// </summary>
        /// <param name="transport">The transport layer.</param>
        /// <param name="serializer">The message serializer.</param>
        public DeliveryActor(ITransport transport, IMessageSerializer serializer)
        {
            this.transport = transport;
            this.serializer = serializer;
            this.inFlight = new Dictionary<Guid, InFlightDelivery>();
        }

        /// <inheritdoc/>
        protected override Behavior Receive(object data)
            => data switch
            {
                DeliveryRequest msg => Actor.Behavior<Unit, DeliveryRequest>(
                    this.HandleDeliveryRequest, msg),
                MessageAccepted msg => Actor.Behavior<Unit, MessageAccepted>(
                    this.HandleMessageAccepted, msg),
                MessageFailed msg => Actor.Behavior<Unit, MessageFailed>(
                    this.HandleMessageFailed, msg),
                PendingMessagesResult msg => Actor.Behavior<Unit, PendingMessagesResult>(
                    this.HandlePendingMessages, msg),
                _ => Actor.Ignore,
            };

        private IObservable<Unit> HandleDeliveryRequest(DeliveryRequest msg)
        {
            // Request pending messages from StateActor for this route.
            var getPending = new GetPendingMessages { Route = msg.Route };

            // Track the satellite for this delivery.
            this.inFlight[Guid.NewGuid()] = new InFlightDelivery
            {
                Route = msg.Route,
                Satellite = msg.Satellite,
            };

            this.SendLocalMessage("state", getPending);
            return Actor.Done;
        }

        private IObservable<Unit> HandlePendingMessages(PendingMessagesResult msg)
        {
            // Find the satellite for this route from in-flight tracking.
            NodeEndpoint satellite = null;
            foreach (var kvp in this.inFlight)
            {
                if (string.Equals(kvp.Value.Route, msg.Route, StringComparison.OrdinalIgnoreCase))
                {
                    satellite = kvp.Value.Satellite;
                    break;
                }
            }

            if (satellite == null || msg.Messages.Count == 0)
            {
                return Actor.Done;
            }

            // Dispatch each pending message to the satellite WorkerActor.
            foreach (var pending in msg.Messages)
            {
                var dispatch = new DispatchWork
                {
                    CorrelationId = pending.CorrelationId,
                    TargetRoute = pending.Route,
                    PayloadTypeName = pending.PayloadTypeName,
                    PayloadBytes = pending.PayloadBytes,
                    OriginNode = pending.OriginNode,
                    IsAsk = pending.IsAsk,
                };

                var deliveryId = Guid.NewGuid();
                this.inFlight[deliveryId] = new InFlightDelivery
                {
                    Route = pending.Route,
                    Satellite = satellite,
                    CorrelationId = pending.CorrelationId,
                    MessageId = pending.MessageId,
                };

                this.SendToSatellite(satellite, dispatch);
            }

            return Actor.Done;
        }

        private IObservable<Unit> HandleMessageAccepted(MessageAccepted msg)
        {
            // Find and remove the in-flight entry.
            Guid? deliveryKey = null;
            InFlightDelivery delivery = null;
            foreach (var kvp in this.inFlight)
            {
                if (kvp.Value.CorrelationId == msg.CorrelationId)
                {
                    deliveryKey = kvp.Key;
                    delivery = kvp.Value;
                    break;
                }
            }

            if (deliveryKey.HasValue && delivery != null)
            {
                this.inFlight.Remove(deliveryKey.Value);

                // Consume the message from StateActor.
                this.SendLocalMessage("state", new ConsumeMessage
                {
                    MessageId = delivery.MessageId,
                    Route = delivery.Route,
                });
            }

            return Actor.Done;
        }

        private IObservable<Unit> HandleMessageFailed(MessageFailed msg)
        {
            // Find and remove the in-flight entry.
            Guid? deliveryKey = null;
            InFlightDelivery delivery = null;
            foreach (var kvp in this.inFlight)
            {
                if (kvp.Value.CorrelationId == msg.CorrelationId)
                {
                    deliveryKey = kvp.Key;
                    delivery = kvp.Value;
                    break;
                }
            }

            if (deliveryKey.HasValue && delivery != null)
            {
                this.inFlight.Remove(deliveryKey.Value);

                // Report failure to RouteActor.
                this.SendLocalMessage("route", new DeliveryResult
                {
                    Route = delivery.Route,
                    Success = false,
                    ErrorMessage = msg.ErrorMessage,
                });
            }

            return Actor.Done;
        }

        private void SendLocalMessage(string targetRoute, object message)
        {
            var envelope = new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetRoute = targetRoute,
                SourceNode = this.transport.LocalEndpoint,
                Type = EnvelopeType.Topology,
                PayloadTypeName = this.serializer.GetTypeName(message),
                PayloadBytes = this.serializer.Serialize(message),
            };

            this.transport.Send(this.transport.LocalEndpoint, envelope)
                .Subscribe(_ => { }, ex => { });
        }

        private void SendToSatellite(NodeEndpoint satellite, object message)
        {
            var envelope = new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetRoute = "worker",
                SourceNode = this.transport.LocalEndpoint,
                Type = EnvelopeType.Topology,
                PayloadTypeName = this.serializer.GetTypeName(message),
                PayloadBytes = this.serializer.Serialize(message),
            };

            this.transport.Send(satellite, envelope)
                .Subscribe(_ => { }, ex => { });
        }

        /// <summary>
        /// Tracks an in-flight message delivery.
        /// </summary>
        private class InFlightDelivery
        {
            /// <summary>
            /// Gets or sets the target route.
            /// </summary>
            public string Route { get; set; }

            /// <summary>
            /// Gets or sets the satellite endpoint.
            /// </summary>
            public NodeEndpoint Satellite { get; set; }

            /// <summary>
            /// Gets or sets the message correlation ID.
            /// </summary>
            public Guid CorrelationId { get; set; }

            /// <summary>
            /// Gets or sets the persisted message ID.
            /// </summary>
            public Guid MessageId { get; set; }
        }
    }
}
