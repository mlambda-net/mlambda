// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LifecycleSteps.cs" company="MLambda">
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

namespace MLambda.Actors.Test.Steps
{
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Test.Actors;
    using Shouldly;
    using TechTalk.SpecFlow;

    /// <summary>
    /// Steps for actor lifecycle hook scenarios.
    /// </summary>
    [Binding]
    public class LifecycleSteps
    {
        private readonly ScenarioContext scenario;

        private readonly IUserContext user;

        /// <summary>
        /// Initializes a new instance of the <see cref="LifecycleSteps"/> class.
        /// </summary>
        /// <param name="scenario">The scenario context.</param>
        /// <param name="user">The user context.</param>
        public LifecycleSteps(ScenarioContext scenario, IUserContext user)
        {
            this.scenario = scenario;
            this.user = user;
        }

        [Given(@"a clean lifecycle log")]
        public void GivenACleanLifecycleLog()
        {
            LifecycleActor.LifecycleLog.Clear();
        }

        [Given(@"a lifecycle actor")]
        public async Task GivenALifecycleActor()
        {
            var address = await this.user.Spawn<LifecycleActor>();
            this.scenario["lifecycle_actor"] = address;
        }

        [When(@"a ping message is sent to the lifecycle actor")]
        public async Task WhenAPingMessageIsSentToTheLifecycleActor()
        {
            var actor = this.scenario.Get<IAddress>("lifecycle_actor");
            var response = await actor.Send<string, string>("ping");
            this.scenario["ping_response"] = response;
        }

        [Then(@"the lifecycle log should contain ""(.*)""")]
        public void ThenTheLifecycleLogShouldContain(string expected)
        {
            LifecycleActor.LifecycleLog.ShouldContain(expected);
        }

        [Then(@"the ping response should be ""(.*)""")]
        public void ThenThePingResponseShouldBe(string expected)
        {
            var actual = this.scenario.Get<string>("ping_response");
            actual.ShouldBe(expected);
        }
    }
}
