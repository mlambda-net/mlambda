// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StashSteps.cs" company="MLambda">
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Test.Actors;
    using MLambda.Actors.Test.Actors.Command;
    using Shouldly;
    using Reqnroll;

    /// <summary>
    /// Steps for message stashing scenarios.
    /// </summary>
    [Binding]
    public class StashSteps
    {
        private readonly ScenarioContext scenario;

        private readonly IUserContext user;

        /// <summary>
        /// Initializes a new instance of the <see cref="StashSteps"/> class.
        /// </summary>
        /// <param name="scenario">The scenario context.</param>
        /// <param name="user">The user context.</param>
        public StashSteps(ScenarioContext scenario, IUserContext user)
        {
            this.scenario = scenario;
            this.user = user;
        }

        [Given(@"a stash actor")]
        public async Task GivenAStashActor()
        {
            var address = await this.user.Spawn<StashActor>();
            this.scenario["stash_actor"] = address;
        }

        [When(@"the messages ""(.*)"" are sent")]
        public async Task WhenTheMessagesAreSent(string messagesCsv)
        {
            var actor = this.scenario.Get<IAddress>("stash_actor");
            var messages = messagesCsv.Split(',').Select(m => m.Trim().Trim('"')).ToList();
            foreach (var message in messages)
            {
                await actor.Send<string, string>(message);
            }
        }

        [When(@"the actor is initialized")]
        public async Task WhenTheActorIsInitialized()
        {
            var actor = this.scenario.Get<IAddress>("stash_actor");
            await actor.Send<Initialize, string>(new Initialize());
        }

        [When(@"the processed messages are queried")]
        public async Task WhenTheProcessedMessagesAreQueried()
        {
            var actor = this.scenario.Get<IAddress>("stash_actor");
            var processed = await actor.Send<GetProcessed, List<string>>(new GetProcessed());
            this.scenario["processed_messages"] = processed;
        }

        [Then(@"the processed messages should be ""(.*)""")]
        public void ThenTheProcessedMessagesShouldBe(string expectedCsv)
        {
            var expected = expectedCsv.Split(',').Select(m => m.Trim().Trim('"')).ToList();
            var actual = this.scenario.Get<List<string>>("processed_messages");
            actual.Count.ShouldBe(expected.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                actual[i].ShouldBe(expected[i]);
            }
        }
    }
}
