// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CounterSteps.cs" company="MLambda">
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
    using MLambda.Actors.Test.Actors.Command;
    using Shouldly;
    using Reqnroll;

    /// <summary>
    /// Steps for stateful counter actor scenarios.
    /// </summary>
    [Binding]
    public class CounterSteps
    {
        private readonly ScenarioContext scenario;

        private readonly IUserContext user;

        /// <summary>
        /// Initializes a new instance of the <see cref="CounterSteps"/> class.
        /// </summary>
        /// <param name="scenario">The scenario context.</param>
        /// <param name="user">The user context.</param>
        public CounterSteps(ScenarioContext scenario, IUserContext user)
        {
            this.scenario = scenario;
            this.user = user;
        }

        [Given(@"a counter actor")]
        public async Task GivenACounterActor()
        {
            var address = await this.user.Spawn<CounterActor>();
            this.scenario["counter_actor"] = address;
        }

        [When(@"the count is queried")]
        public async Task WhenTheCountIsQueried()
        {
            var actor = this.scenario.Get<IAddress>("counter_actor");
            var count = await actor.Send<GetCount, int>(new GetCount());
            this.scenario["current_count"] = count;
        }

        [When(@"the counter is incremented (\d+) times")]
        public async Task WhenTheCounterIsIncrementedTimes(int times)
        {
            var actor = this.scenario.Get<IAddress>("counter_actor");
            int lastResult = 0;
            for (int i = 0; i < times; i++)
            {
                lastResult = await actor.Send<Increment, int>(new Increment());
            }

            this.scenario["last_increment_result"] = lastResult;
        }

        [When(@"the counter is decremented (\d+) times")]
        public async Task WhenTheCounterIsDecrementedTimes(int times)
        {
            var actor = this.scenario.Get<IAddress>("counter_actor");
            for (int i = 0; i < times; i++)
            {
                await actor.Send<Decrement, int>(new Decrement());
            }
        }

        [Then(@"the count should be (\d+)")]
        public void ThenTheCountShouldBe(int expected)
        {
            var actual = this.scenario.Get<int>("current_count");
            actual.ShouldBe(expected);
        }

        [Then(@"the increment response should be (\d+)")]
        public void ThenTheIncrementResponseShouldBe(int expected)
        {
            var actual = this.scenario.Get<int>("last_increment_result");
            actual.ShouldBe(expected);
        }
    }
}
