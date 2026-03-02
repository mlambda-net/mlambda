// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BecomeSteps.cs" company="MLambda">
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
    using TechTalk.SpecFlow;

    /// <summary>
    /// Steps for Become/Unbecome behavior switching scenarios.
    /// </summary>
    [Binding]
    public class BecomeSteps
    {
        private readonly ScenarioContext scenario;

        private readonly IUserContext user;

        /// <summary>
        /// Initializes a new instance of the <see cref="BecomeSteps"/> class.
        /// </summary>
        /// <param name="scenario">The scenario context.</param>
        /// <param name="user">The user context.</param>
        public BecomeSteps(ScenarioContext scenario, IUserContext user)
        {
            this.scenario = scenario;
            this.user = user;
        }

        [Given(@"a become actor")]
        public async Task GivenABecomeActor()
        {
            var address = await this.user.Spawn<BecomeActor>();
            this.scenario["become_actor"] = address;
        }

        [When(@"the mood is set to ""(.*)""")]
        public async Task WhenTheMoodIsSetTo(string mood)
        {
            var actor = this.scenario.Get<IAddress>("become_actor");
            await actor.Send(new SetMood(mood));
        }

        [When(@"the mood is queried")]
        public async Task WhenTheMoodIsQueried()
        {
            var actor = this.scenario.Get<IAddress>("become_actor");
            var mood = await actor.Send<AskMood, string>(new AskMood());
            this.scenario["current_mood"] = mood;
        }

        [Then(@"the mood should be ""(.*)""")]
        public void ThenTheMoodShouldBe(string expected)
        {
            var actual = this.scenario.Get<string>("current_mood");
            actual.ShouldBe(expected);
        }
    }
}
