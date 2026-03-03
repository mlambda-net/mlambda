// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActorMetrics.cs" company="MLambda">
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
    using Prometheus;

    /// <summary>
    /// Prometheus metric definitions for actor monitoring.
    /// </summary>
    public static class ActorMetrics
    {
        /// <summary>
        /// Total number of messages processed by actors.
        /// </summary>
        public static readonly Counter MessagesTotal = Metrics.CreateCounter(
            "mlambda_actor_messages_total",
            "Total number of messages processed by actors.",
            new CounterConfiguration
            {
                LabelNames = new[] { "route", "message_type", "node_id" },
            });

        /// <summary>
        /// Total number of errors during actor message processing.
        /// </summary>
        public static readonly Counter ErrorsTotal = Metrics.CreateCounter(
            "mlambda_actor_errors_total",
            "Total number of errors during actor message processing.",
            new CounterConfiguration
            {
                LabelNames = new[] { "route", "message_type", "node_id", "error_type" },
            });

        /// <summary>
        /// Duration of actor message processing in seconds.
        /// </summary>
        public static readonly Histogram MessageDuration = Metrics.CreateHistogram(
            "mlambda_actor_message_duration_seconds",
            "Duration of actor message processing in seconds.",
            new HistogramConfiguration
            {
                LabelNames = new[] { "route", "message_type", "node_id" },
                Buckets = new[] { 0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0 },
            });

        /// <summary>
        /// Number of active actors on the node.
        /// </summary>
        public static readonly Gauge ActiveActors = Metrics.CreateGauge(
            "mlambda_actor_active_count",
            "Number of active actors on the node.",
            new GaugeConfiguration
            {
                LabelNames = new[] { "node_id" },
            });
    }
}
