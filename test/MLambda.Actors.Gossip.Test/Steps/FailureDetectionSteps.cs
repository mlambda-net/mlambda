// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FailureDetectionSteps.cs" company="MLambda">
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

namespace MLambda.Actors.Gossip.Test.Steps
{
    using System;
    using System.Threading;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for failure detection tests.
    /// </summary>
    [Binding]
    public class FailureDetectionSteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="FailureDetectionSteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public FailureDetectionSteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates a phi accrual failure detector with the given threshold.
        /// </summary>
        /// <param name="threshold">The phi threshold.</param>
        [Given(@"a phi accrual failure detector with threshold (.*)")]
        public void GivenAPhiAccrualFailureDetectorWithThreshold(double threshold)
        {
            var detector = new PhiAccrualFailureDetector(threshold);
            this.context["detector"] = detector;
            this.context["nodeX"] = Guid.NewGuid();
        }

        /// <summary>
        /// Sends heartbeats at regular intervals.
        /// </summary>
        [When(@"heartbeats are received from node X at regular intervals")]
        public void WhenHeartbeatsAreReceivedAtRegularIntervals()
        {
            var detector = this.context.Get<PhiAccrualFailureDetector>("detector");
            var nodeId = this.context.Get<Guid>("nodeX");

            for (int i = 0; i < 10; i++)
            {
                detector.Heartbeat(nodeId);
                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// Sends heartbeats then stops to simulate failure.
        /// </summary>
        [When(@"heartbeats are received from node X then stopped")]
        public void WhenHeartbeatsAreReceivedThenStopped()
        {
            var detector = this.context.Get<PhiAccrualFailureDetector>("detector");
            var nodeId = this.context.Get<Guid>("nodeX");

            for (int i = 0; i < 10; i++)
            {
                detector.Heartbeat(nodeId);
                Thread.Sleep(30);
            }
        }

        /// <summary>
        /// Waits for suspicion level to rise above threshold.
        /// </summary>
        [When(@"we wait for the suspicion to rise")]
        public void WhenWeWaitForTheSuspicionToRise()
        {
            Thread.Sleep(2000);
        }

        /// <summary>
        /// Verifies that node X is available.
        /// </summary>
        [Then(@"node X should be available")]
        public void ThenNodeXShouldBeAvailable()
        {
            var detector = this.context.Get<PhiAccrualFailureDetector>("detector");
            var nodeId = this.context.Get<Guid>("nodeX");
            detector.IsAvailable(nodeId).ShouldBeTrue();
        }

        /// <summary>
        /// Verifies that node X is not available.
        /// </summary>
        [Then(@"node X should not be available")]
        public void ThenNodeXShouldNotBeAvailable()
        {
            var detector = this.context.Get<PhiAccrualFailureDetector>("detector");
            var nodeId = this.context.Get<Guid>("nodeX");
            detector.IsAvailable(nodeId).ShouldBeFalse();
        }
    }
}
