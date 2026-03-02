// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeathWatchSteps.cs" company="MLambda">
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
    using System.Threading;
    using System.Threading.Tasks;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Context;
    using MLambda.Actors.Abstraction.Core;
    using MLambda.Actors.Guardian.Messages;
    using MLambda.Actors.Test.Actors;
    using MLambda.Actors.Test.Actors.Command;
    using Shouldly;
    using Reqnroll;

    /// <summary>
    /// Steps for DeathWatch monitoring scenarios.
    /// </summary>
    [Binding]
    public class DeathWatchSteps
    {
        private readonly ScenarioContext scenario;

        private readonly IUserContext user;

        private readonly ISystemContext system;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeathWatchSteps"/> class.
        /// </summary>
        /// <param name="scenario">The scenario context.</param>
        /// <param name="user">The user context.</param>
        /// <param name="system">The system context.</param>
        public DeathWatchSteps(ScenarioContext scenario, IUserContext user, ISystemContext system)
        {
            this.scenario = scenario;
            this.user = user;
            this.system = system;
        }

        [Given(@"a watcher actor")]
        public async Task GivenAWatcherActor()
        {
            var address = await this.user.Spawn<WatcherActor>();
            this.scenario["watcher"] = address;
        }

        [Given(@"a target actor to watch")]
        public async Task GivenATargetActorToWatch()
        {
            var address = await this.user.Spawn<ConsoleActor>();
            this.scenario["target"] = address;
        }

        [When(@"the watcher watches the target")]
        public async Task WhenTheWatcherWatchesTheTarget()
        {
            var watcher = this.scenario.Get<IAddress>("watcher");
            var target = this.scenario.Get<IAddress>("target");
            await watcher.Send(new WatchTarget(target));
        }

        [When(@"the watcher unwatches the target")]
        public async Task WhenTheWatcherUnwatchesTheTarget()
        {
            var watcher = this.scenario.Get<IAddress>("watcher");
            await watcher.Send(new UnwatchTarget());
        }

        [When(@"the target actor is stopped")]
        public async Task WhenTheTargetActorIsStopped()
        {
            var target = this.scenario.Get<IAddress>("target");
            var processes = await this.system.Self.Send<ProcessFilter, IEnumerable<Pid>>(
                new ProcessFilter("/user/console"));
            var pid = processes.FirstOrDefault();
            if (pid != null)
            {
                await this.system.Self.Send(new Kill(pid.Id));
            }
        }

        [When(@"we wait for the terminated notification")]
        public void WhenWeWaitForTheTerminatedNotification()
        {
            Thread.Sleep(500);
        }

        [Then(@"the watcher should have received a terminated message")]
        public async Task ThenTheWatcherShouldHaveReceivedATerminatedMessage()
        {
            var watcher = this.scenario.Get<IAddress>("watcher");
            var terminated = await watcher.Send<IsTerminated, bool>(new IsTerminated());
            terminated.ShouldBeTrue();
        }

        [Then(@"the watcher should not have received a terminated message")]
        public async Task ThenTheWatcherShouldNotHaveReceivedATerminatedMessage()
        {
            var watcher = this.scenario.Get<IAddress>("watcher");
            var terminated = await watcher.Send<IsTerminated, bool>(new IsTerminated());
            terminated.ShouldBeFalse();
        }
    }
}
