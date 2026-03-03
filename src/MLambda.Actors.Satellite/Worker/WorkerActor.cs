// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WorkerActor.cs" company="MLambda">
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

namespace MLambda.Actors.Satellite.Worker
{
    using System;
    using System.Collections.Concurrent;
    using System.Reactive;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Cluster.Abstraction;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Satellite.Abstraction;

    /// <summary>
    /// Satellite-side parent actor that supervises all user actors.
    /// Handles actor creation, message delivery with failure tracking,
    /// and sends MessageAccepted/MessageFailed back to the cluster
    /// DeliveryActor for delivery coordination.
    /// </summary>
    [Route("worker")]
    public class WorkerActor : Actor
    {
        private readonly ActorTypeRegistry typeRegistry;
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;
        private readonly ActorCatalogConfig config;

        private readonly ConcurrentDictionary<string, IAddress> childActors;
        private readonly ConcurrentDictionary<string, int> failureCounts;
        private readonly int maxFailures = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerActor"/> class.
        /// </summary>
        /// <param name="typeRegistry">The actor type registry for route-to-type resolution.</param>
        /// <param name="transport">The transport layer for sending results back.</param>
        /// <param name="serializer">The message serializer.</param>
        /// <param name="config">The actor address configuration.</param>
        public WorkerActor(
            ActorTypeRegistry typeRegistry,
            ITransport transport,
            IMessageSerializer serializer,
            ActorCatalogConfig config)
        {
            this.typeRegistry = typeRegistry;
            this.transport = transport;
            this.serializer = serializer;
            this.config = config;

            this.childActors = new ConcurrentDictionary<string, IAddress>(StringComparer.OrdinalIgnoreCase);
            this.failureCounts = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        protected override Behavior Receive(object data)
            => data switch
            {
                ActorCreate msg => Actor.Behavior<Unit, ActorCreate>(this.HandleActorCreate, msg),
                DispatchWork msg => Actor.Behavior<Unit, DispatchWork>(this.HandleDispatchWork, msg),
                _ => Actor.Ignore,
            };

        private IObservable<Unit> HandleActorCreate(IContext context, ActorCreate msg)
        {
            try
            {
                var child = this.SpawnChild(context, msg.TargetRoute);
                if (child == null)
                {
                    this.SendActorCreateResult(msg, false, "Unknown route: " + msg.TargetRoute);
                    return Actor.Done;
                }

                this.SendActorCreateResult(msg, true, null);
            }
            catch (Exception ex)
            {
                this.SendActorCreateResult(msg, false, ex.Message);
            }

            return Actor.Done;
        }

        private IObservable<Unit> HandleDispatchWork(IContext context, DispatchWork msg)
        {
            try
            {
                var child = this.EnsureChild(context, msg.TargetRoute);
                if (child == null)
                {
                    this.SendMessageFailed(msg, "Unknown route: " + msg.TargetRoute);
                    return Actor.Done;
                }

                var payload = this.serializer.Deserialize(msg.PayloadBytes, msg.PayloadTypeName);

                if (msg.IsAsk)
                {
                    child.Send<object, object>(payload).Subscribe(
                        response =>
                        {
                            this.HandleChildResponse(msg, response);
                        },
                        ex =>
                        {
                            this.OnChildFailure(context, msg, ex);
                        });
                }
                else
                {
                    child.Send(payload).Subscribe(
                        result =>
                        {
                            this.HandleChildTellResponse(msg, result);
                        },
                        ex =>
                        {
                            this.OnChildFailure(context, msg, ex);
                        });
                }
            }
            catch (Exception ex)
            {
                this.SendMessageFailed(msg, ex.Message);
            }

            return Actor.Done;
        }

        private IAddress SpawnChild(IContext context, string route)
        {
            if (this.childActors.TryGetValue(route, out var existing))
            {
                return existing;
            }

            if (!this.typeRegistry.TryGetType(route, out var actorType))
            {
                return null;
            }

            IAddress address = null;
            context.Spawn(actorType).Subscribe(a => address = a);
            if (address != null)
            {
                this.childActors[route] = address;
            }

            return address;
        }

        private IAddress EnsureChild(IContext context, string route)
        {
            if (this.childActors.TryGetValue(route, out var existing))
            {
                return existing;
            }

            return this.SpawnChild(context, route);
        }

        private void HandleChildResponse(DispatchWork msg, object response)
        {
            if (response is StateResult stateResult)
            {
                this.SendResponseToOrigin(msg, stateResult.Value);
                if (stateResult.Decision == StateDecision.Flush)
                {
                    this.OnChildSuccess(msg);
                    this.SendFlushToCluster(msg);
                }

                // Keep: do not call OnChildSuccess — message stays in persistent storage for retry.
            }
            else
            {
                this.OnChildSuccess(msg);
                this.SendResponseToOrigin(msg, response);
            }
        }

        private void HandleChildTellResponse(DispatchWork msg, object result)
        {
            if (result is StateResult stateResult)
            {
                if (stateResult.Decision == StateDecision.Flush)
                {
                    this.OnChildSuccess(msg);
                    this.SendFlushToCluster(msg);
                }

                // Keep: do not call OnChildSuccess — message stays in persistent storage for retry.
            }
            else
            {
                this.OnChildSuccess(msg);
            }
        }

        private void OnChildSuccess(DispatchWork msg)
        {
            this.failureCounts.TryRemove(msg.TargetRoute, out _);
            this.SendMessageAccepted(msg);
        }

        private void OnChildFailure(IContext context, DispatchWork msg, Exception ex)
        {
            var failures = this.failureCounts.AddOrUpdate(msg.TargetRoute, 1, (_, count) => count + 1);

            if (failures >= this.maxFailures)
            {
                this.failureCounts.TryRemove(msg.TargetRoute, out _);
                this.SendMessageFailed(msg, $"Exceeded {this.maxFailures} failures: {ex.Message}");
            }
            else
            {
                this.RestartChild(context, msg.TargetRoute);
                this.SendMessageFailed(msg, ex.Message);
            }
        }

        private void RestartChild(IContext context, string route)
        {
            if (this.childActors.TryRemove(route, out var old))
            {
                old.Dispose();
            }

            this.SpawnChild(context, route);
        }

        private void SendMessageAccepted(DispatchWork msg)
        {
            var accepted = new MessageAccepted
            {
                CorrelationId = msg.CorrelationId,
                Route = msg.TargetRoute,
            };

            this.SendToCluster("delivery", accepted);
        }

        private void SendMessageFailed(DispatchWork msg, string error)
        {
            var failed = new MessageFailed
            {
                CorrelationId = msg.CorrelationId,
                Route = msg.TargetRoute,
                ErrorMessage = error,
            };

            this.SendToCluster("delivery", failed);
        }

        private void SendFlushToCluster(DispatchWork msg)
        {
            var flush = new FlushMessage
            {
                CorrelationId = msg.CorrelationId,
                Route = msg.TargetRoute,
            };

            this.SendToCluster("state", flush);
        }

        private void SendResponseToOrigin(DispatchWork msg, object response)
        {
            if (msg.OriginNode == null)
            {
                return;
            }

            var responseEnvelope = new Envelope
            {
                CorrelationId = msg.CorrelationId,
                SourceNode = this.config.LocalEndpoint,
                Type = EnvelopeType.Response,
                PayloadTypeName = this.serializer.GetTypeName(response),
                PayloadBytes = this.serializer.Serialize(response),
            };

            this.transport.Send(msg.OriginNode, responseEnvelope)
                .Subscribe(_ => { }, ex => { });
        }

        private void SendActorCreateResult(ActorCreate msg, bool success, string error)
        {
            var result = new ActorCreateResult
            {
                CorrelationId = msg.CorrelationId,
                TargetRoute = msg.TargetRoute,
                Success = success,
                ErrorMessage = error,
            };

            if (msg.OriginNode != null)
            {
                this.SendToNode(msg.OriginNode, "route", result);
            }
        }

        private void SendToCluster(string targetRoute, object message)
        {
            var clusterNodes = this.config.ClusterNodes;
            if (clusterNodes == null || clusterNodes.Count == 0)
            {
                return;
            }

            foreach (var clusterNode in clusterNodes)
            {
                try
                {
                    this.SendToNode(clusterNode, targetRoute, message);
                }
                catch (Exception)
                {
                    // Ignore failures sending to cluster.
                }
            }
        }

        private void SendToNode(NodeEndpoint node, string targetRoute, object message)
        {
            var envelope = new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetRoute = targetRoute,
                SourceNode = this.config.LocalEndpoint,
                Type = EnvelopeType.Topology,
                PayloadTypeName = this.serializer.GetTypeName(message),
                PayloadBytes = this.serializer.Serialize(message),
            };

            this.transport.Send(node, envelope)
                .Subscribe(_ => { }, ex => { });
        }
    }
}
