// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClusterMessages.cs" company="MLambda">
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

namespace MLambda.Actors.Cluster.Abstraction
{
    using System;
    using System.Collections.Generic;
    using MLambda.Actors.Network.Abstraction;

    // ---- Satellite Registration Messages ----

    /// <summary>
    /// Sent by a satellite to register with a cluster node.
    /// </summary>
    public class SatelliteRegister
    {
        /// <summary>
        /// Gets or sets the satellite's endpoint.
        /// </summary>
        public NodeEndpoint SatelliteEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the list of actor routes the satellite can host.
        /// </summary>
        public List<string> Capabilities { get; set; } = new List<string>();
    }

    /// <summary>
    /// Sent back to a satellite to confirm registration.
    /// </summary>
    public class SatelliteRegistered
    {
        /// <summary>
        /// Gets or sets a value indicating whether the registration was successful.
        /// </summary>
        public bool Success { get; set; }
    }

    /// <summary>
    /// Periodic heartbeat from a satellite to the cluster.
    /// </summary>
    public class SatelliteHeartbeat
    {
        /// <summary>
        /// Gets or sets the satellite's endpoint.
        /// </summary>
        public NodeEndpoint SatelliteEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the current load of the satellite.
        /// </summary>
        public int Load { get; set; }
    }

    /// <summary>
    /// Indicates a satellite has disconnected or is shutting down.
    /// </summary>
    public class SatelliteDisconnected
    {
        /// <summary>
        /// Gets or sets the satellite's endpoint.
        /// </summary>
        public NodeEndpoint SatelliteEndpoint { get; set; }
    }

    // ---- Work Dispatch Messages ----

    /// <summary>
    /// Dispatches work from a cluster node to a satellite WorkerActor.
    /// </summary>
    public class DispatchWork
    {
        /// <summary>
        /// Gets or sets the correlation ID for tracking the request.
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the target route for the actor.
        /// </summary>
        public string TargetRoute { get; set; }

        /// <summary>
        /// Gets or sets the type name of the serialized payload.
        /// </summary>
        public string PayloadTypeName { get; set; }

        /// <summary>
        /// Gets or sets the serialized payload bytes.
        /// </summary>
        public byte[] PayloadBytes { get; set; }

        /// <summary>
        /// Gets or sets the originating node.
        /// </summary>
        public NodeEndpoint OriginNode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is an Ask (request-response) message.
        /// </summary>
        public bool IsAsk { get; set; }
    }

    /// <summary>
    /// Result of work execution on a satellite.
    /// </summary>
    public class WorkResult
    {
        /// <summary>
        /// Gets or sets the correlation ID of the original request.
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the type name of the serialized response payload.
        /// </summary>
        public string PayloadTypeName { get; set; }

        /// <summary>
        /// Gets or sets the serialized response payload bytes.
        /// </summary>
        public byte[] PayloadBytes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether execution succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if execution failed.
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    // ---- Actor Creation Messages ----

    /// <summary>
    /// Requests creation of an actor on a satellite node.
    /// </summary>
    public class ActorCreate
    {
        /// <summary>
        /// Gets or sets the correlation ID for tracking the creation.
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the target route for the actor to create.
        /// </summary>
        public string TargetRoute { get; set; }

        /// <summary>
        /// Gets or sets the originating node.
        /// </summary>
        public NodeEndpoint OriginNode { get; set; }
    }

    /// <summary>
    /// Result of an actor creation request.
    /// </summary>
    public class ActorCreateResult
    {
        /// <summary>
        /// Gets or sets the correlation ID of the original request.
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the target route of the created actor.
        /// </summary>
        public string TargetRoute { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether creation succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if creation failed.
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    // ---- Delivery Messages (RouteActor <-> DeliveryActor) ----

    /// <summary>
    /// Sent by RouteActor to DeliveryActor to begin delivering messages for a route.
    /// </summary>
    public class DeliveryRequest
    {
        /// <summary>
        /// Gets or sets the route to deliver messages for.
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// Gets or sets the satellite hosting the actor.
        /// </summary>
        public NodeEndpoint Satellite { get; set; }
    }

    /// <summary>
    /// Sent by DeliveryActor back to RouteActor with delivery outcome.
    /// </summary>
    public class DeliveryResult
    {
        /// <summary>
        /// Gets or sets the route that was being delivered.
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether delivery succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if delivery failed.
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    // ---- State Persistence Messages ----

    /// <summary>
    /// Persist a message in the StateActor for durability.
    /// </summary>
    public class PersistMessage
    {
        /// <summary>
        /// Gets or sets the unique message identifier.
        /// </summary>
        public Guid MessageId { get; set; }

        /// <summary>
        /// Gets or sets the target route.
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// Gets or sets the type name of the serialized payload.
        /// </summary>
        public string PayloadTypeName { get; set; }

        /// <summary>
        /// Gets or sets the serialized payload bytes.
        /// </summary>
        public byte[] PayloadBytes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is an Ask message.
        /// </summary>
        public bool IsAsk { get; set; }

        /// <summary>
        /// Gets or sets the correlation ID.
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the originating node.
        /// </summary>
        public NodeEndpoint OriginNode { get; set; }

        /// <summary>
        /// Gets or sets the time the message was persisted.
        /// </summary>
        public DateTimeOffset PersistedAt { get; set; }
    }

    /// <summary>
    /// Consume (remove) a message from the StateActor after successful delivery.
    /// </summary>
    public class ConsumeMessage
    {
        /// <summary>
        /// Gets or sets the message ID to consume.
        /// </summary>
        public Guid MessageId { get; set; }

        /// <summary>
        /// Gets or sets the route the message belongs to.
        /// </summary>
        public string Route { get; set; }
    }

    /// <summary>
    /// Request all pending messages for a route from the StateActor.
    /// </summary>
    public class GetPendingMessages
    {
        /// <summary>
        /// Gets or sets the route to get pending messages for.
        /// </summary>
        public string Route { get; set; }
    }

    /// <summary>
    /// Response containing pending messages for a route.
    /// </summary>
    public class PendingMessagesResult
    {
        /// <summary>
        /// Gets or sets the route.
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// Gets or sets the list of pending messages.
        /// </summary>
        public List<PersistMessage> Messages { get; set; } = new List<PersistMessage>();
    }

    // ---- Worker Response Messages ----

    /// <summary>
    /// Sent by a satellite WorkerActor to indicate a message was successfully processed.
    /// </summary>
    public class MessageAccepted
    {
        /// <summary>
        /// Gets or sets the correlation ID of the processed message.
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the route of the actor that processed the message.
        /// </summary>
        public string Route { get; set; }
    }

    /// <summary>
    /// Sent by a satellite WorkerActor to indicate a message processing failed.
    /// </summary>
    public class MessageFailed
    {
        /// <summary>
        /// Gets or sets the correlation ID of the failed message.
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the route of the actor that failed.
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
