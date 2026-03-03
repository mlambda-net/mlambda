// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstrumentedSupervisor.cs" company="MLambda">
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

namespace MLambda.Actors.Monitoring
{
    using System;
    using System.Diagnostics;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Abstraction.Supervision;
    using OpenTelemetry.Trace;

    /// <summary>
    /// A supervisor decorator that transparently instruments all actor message processing
    /// with Prometheus metrics, OpenTelemetry distributed tracing, and structured console logging.
    /// </summary>
    public class InstrumentedSupervisor : ISupervisor
    {
        private readonly ISupervisor inner;

        private readonly string nodeId;

        private readonly bool enableTracing;

        private readonly bool enableConsoleLogging;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstrumentedSupervisor"/> class.
        /// </summary>
        /// <param name="inner">The inner supervisor to delegate to.</param>
        /// <param name="config">The monitoring configuration.</param>
        public InstrumentedSupervisor(ISupervisor inner, MonitoringConfig config)
        {
            this.inner = inner;
            this.nodeId = config.NodeId;
            this.enableTracing = config.EnableTracing;
            this.enableConsoleLogging = config.EnableConsoleLogging;
        }

        /// <summary>
        /// Applies the message with instrumentation, recording metrics, tracing spans,
        /// and console logs before delegating error handling to the inner supervisor.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <returns>A function that processes the message in context.</returns>
        public Func<IMainContext, Task> Apply(IMessage message) =>
            async context =>
            {
                var route = context.Process?.Route ?? "unknown";
                var actorName = context.Actor?.GetType().Name ?? "unknown";
                var messageType = message.Payload?.GetType().Name ?? "null";
                var messageName = message.Payload?.GetType().FullName ?? "null";
                var requestId = message.RequestId;
                var messageKind = message.GetType().Name == "Synchronous" ? "Ask" : "Tell";
                var stopwatch = Stopwatch.StartNew();

                Activity activity = null;
                if (this.enableTracing)
                {
                    activity = ActorTracing.Source.StartActivity(
                        "actor.receive",
                        ActivityKind.Internal);

                    if (activity != null)
                    {
                        activity.SetTag("actor.name", actorName);
                        activity.SetTag("actor.route", route);
                        activity.SetTag("message.type", messageType);
                        activity.SetTag("message.name", messageName);
                        activity.SetTag("message.kind", messageKind);
                        activity.SetTag("message.request_id", requestId.ToString());
                        activity.SetTag("node.id", this.nodeId);
                    }
                }

                if (this.enableConsoleLogging)
                {
                    Console.WriteLine(
                        $"[RECV] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} | {requestId} | {route} | {actorName} | {messageType} | {messageKind}");
                }

                try
                {
                    message.Response(await context.Actor.Receive(message.Payload)(context));
                    stopwatch.Stop();

                    ActorMetrics.MessagesTotal
                        .WithLabels(route, messageType, this.nodeId)
                        .Inc();
                    ActorMetrics.MessageDuration
                        .WithLabels(route, messageType, this.nodeId)
                        .Observe(stopwatch.Elapsed.TotalSeconds);

                    if (this.enableConsoleLogging)
                    {
                        Console.WriteLine(
                            $"[DONE] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} | {requestId} | {route} | {actorName} | {messageType} | {stopwatch.Elapsed.TotalMilliseconds:F1}ms");
                    }

                    activity?.SetStatus(ActivityStatusCode.Ok);
                }
                catch (Exception exception)
                {
                    stopwatch.Stop();

                    ActorMetrics.MessagesTotal
                        .WithLabels(route, messageType, this.nodeId)
                        .Inc();
                    ActorMetrics.ErrorsTotal
                        .WithLabels(route, messageType, this.nodeId, exception.GetType().Name)
                        .Inc();
                    ActorMetrics.MessageDuration
                        .WithLabels(route, messageType, this.nodeId)
                        .Observe(stopwatch.Elapsed.TotalSeconds);

                    if (this.enableConsoleLogging)
                    {
                        Console.WriteLine(
                            $"[FAIL] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} | {requestId} | {route} | {actorName} | {messageType} | {exception.GetType().Name}: {exception.Message}");
                    }

                    if (activity != null)
                    {
                        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                        activity.RecordException(exception);
                    }

                    this.inner.Handle(exception, context.Process);
                }
                finally
                {
                    activity?.Dispose();
                }
            };

        /// <summary>
        /// Handles an exception by delegating to the inner supervisor.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="process">The process.</param>
        public void Handle(Exception exception, IProcess process)
        {
            this.inner.Handle(exception, process);
        }
    }
}
