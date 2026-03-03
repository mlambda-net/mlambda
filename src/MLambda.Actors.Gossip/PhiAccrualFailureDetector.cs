// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PhiAccrualFailureDetector.cs" company="MLambda">
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

namespace MLambda.Actors.Gossip
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using MLambda.Actors.Gossip.Abstraction;

    /// <summary>
    /// Phi accrual failure detector using heartbeat inter-arrival times.
    /// </summary>
    public class PhiAccrualFailureDetector : IFailureDetector
    {
        private const int WindowSize = 100;
        private const double MinStdDeviation = 100.0;

        private readonly ConcurrentDictionary<string, HeartbeatHistory> histories;
        private readonly double threshold;

        /// <summary>
        /// Initializes a new instance of the <see cref="PhiAccrualFailureDetector"/> class.
        /// </summary>
        /// <param name="threshold">The phi threshold.</param>
        public PhiAccrualFailureDetector(double threshold)
        {
            this.threshold = threshold;
            this.histories = new ConcurrentDictionary<string, HeartbeatHistory>();
        }

        /// <inheritdoc/>
        public void Heartbeat(string nodeId)
        {
            var history = this.histories.GetOrAdd(nodeId, _ => new HeartbeatHistory());
            history.Add(DateTimeOffset.UtcNow);
        }

        /// <inheritdoc/>
        public bool IsAvailable(string nodeId)
        {
            return this.GetSuspicionLevel(nodeId) < this.threshold;
        }

        /// <inheritdoc/>
        public double GetSuspicionLevel(string nodeId)
        {
            if (!this.histories.TryGetValue(nodeId, out var history))
            {
                return 0.0;
            }

            return history.Phi(DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Sliding window heartbeat history for phi computation.
        /// </summary>
        private class HeartbeatHistory
        {
            private readonly object locker = new object();
            private readonly List<double> intervals = new List<double>();
            private DateTimeOffset lastHeartbeat;
            private bool hasFirstBeat;

            /// <summary>
            /// Adds a heartbeat timestamp.
            /// </summary>
            /// <param name="timestamp">The heartbeat timestamp.</param>
            public void Add(DateTimeOffset timestamp)
            {
                lock (this.locker)
                {
                    if (this.hasFirstBeat)
                    {
                        var interval = (timestamp - this.lastHeartbeat).TotalMilliseconds;
                        this.intervals.Add(interval);
                        if (this.intervals.Count > WindowSize)
                        {
                            this.intervals.RemoveAt(0);
                        }
                    }

                    this.lastHeartbeat = timestamp;
                    this.hasFirstBeat = true;
                }
            }

            /// <summary>
            /// Computes the phi value at the given timestamp.
            /// </summary>
            /// <param name="now">The current time.</param>
            /// <returns>The phi value.</returns>
            public double Phi(DateTimeOffset now)
            {
                lock (this.locker)
                {
                    if (!this.hasFirstBeat || this.intervals.Count == 0)
                    {
                        return 0.0;
                    }

                    var elapsed = (now - this.lastHeartbeat).TotalMilliseconds;
                    var mean = this.intervals.Average();
                    var variance = this.intervals.Select(x => Math.Pow(x - mean, 2)).Average();
                    var stdDev = Math.Max(Math.Sqrt(variance), MinStdDeviation);

                    var y = (elapsed - mean) / stdDev;
                    var e = Math.Exp(-y * (Math.PI / Math.Sqrt(6.0)));
                    var cdf = 1.0 / (1.0 + e);

                    return -Math.Log10(1.0 - cdf);
                }
            }
        }
    }
}
