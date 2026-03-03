// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FortressClock.cs" company="MLambda">
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

namespace MLambda.Actors.Fortress
{
    using System;
    using System.Reactive.Linq;
    using System.Threading;
    using Cronos;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// Cron-based timer that triggers certificate rotation on the Fortress CA.
    /// Parses the cron expression from <see cref="FortressConfig.RotationCron"/>
    /// and sends <see cref="RotationTick"/> messages to the sentinel.
    /// </summary>
    public class FortressClock : IDisposable
    {
        private readonly FortressConfig config;
        private readonly ITransport transport;
        private readonly IMessageSerializer serializer;
        private Timer timer;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FortressClock"/> class.
        /// </summary>
        /// <param name="config">The fortress configuration.</param>
        /// <param name="transport">The transport layer for sending tick messages.</param>
        /// <param name="serializer">The message serializer.</param>
        public FortressClock(
            FortressConfig config,
            ITransport transport,
            IMessageSerializer serializer)
        {
            this.config = config;
            this.transport = transport;
            this.serializer = serializer;
        }

        /// <summary>
        /// Starts the rotation clock if a cron expression is configured.
        /// </summary>
        public void Start()
        {
            if (string.IsNullOrWhiteSpace(this.config.RotationCron))
            {
                return;
            }

            this.ScheduleNext();
        }

        /// <summary>
        /// Stops the rotation clock.
        /// </summary>
        public void Stop()
        {
            this.timer?.Change(Timeout.Infinite, Timeout.Infinite);
            this.timer?.Dispose();
            this.timer = null;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes managed resources.
        /// </summary>
        /// <param name="disposing">True if disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed && disposing)
            {
                this.disposed = true;
                this.Stop();
            }
        }

        private void ScheduleNext()
        {
            try
            {
                var cron = CronExpression.Parse(this.config.RotationCron);
                var next = cron.GetNextOccurrence(DateTime.UtcNow);

                if (next.HasValue)
                {
                    var delay = next.Value - DateTime.UtcNow;
                    if (delay < TimeSpan.Zero)
                    {
                        delay = TimeSpan.FromSeconds(1);
                    }

                    this.timer?.Dispose();
                    this.timer = new Timer(this.OnTick, null, delay, Timeout.InfiniteTimeSpan);
                }
            }
            catch (Exception)
            {
                // Invalid cron expression; clock does not start.
            }
        }

        private void OnTick(object state)
        {
            var tick = new RotationTick();
            var envelope = new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetRoute = "fortress-ca",
                SourceNode = this.transport.LocalEndpoint,
                Type = EnvelopeType.Fortress,
                PayloadTypeName = this.serializer.GetTypeName(tick),
                PayloadBytes = this.serializer.Serialize(tick),
            };

            this.transport.Send(this.transport.LocalEndpoint, envelope)
                .Subscribe(_ => { }, ex => { });

            // Reschedule for next occurrence.
            this.ScheduleNext();
        }
    }
}
