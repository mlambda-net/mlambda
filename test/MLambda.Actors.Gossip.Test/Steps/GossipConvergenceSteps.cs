// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GossipConvergenceSteps.cs" company="MLambda">
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
    using MLambda.Actors.Gossip.Abstraction;
    using MLambda.Actors.Network.Abstraction;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for gossip state convergence tests.
    /// </summary>
    [Binding]
    public class GossipConvergenceSteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="GossipConvergenceSteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public GossipConvergenceSteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates a gossip state with a member at the given status.
        /// </summary>
        /// <param name="name">The member name.</param>
        /// <param name="status">The member status.</param>
        [Given(@"a gossip state with member (.*) as (.*)")]
        public void GivenAGossipStateWithMemberAsStatus(string name, string status)
        {
            var nodeId = this.GetOrCreateNodeId(name);
            var endpoint = new NodeEndpoint(nodeId, 9000);
            var memberStatus = Enum.Parse<MemberStatus>(status);
            var member = new Member(endpoint, memberStatus);
            var state = new GossipState();
            state.Members[nodeId] = member;
            this.context["state1"] = state;
        }

        /// <summary>
        /// Creates another gossip state with a member at the given status.
        /// </summary>
        /// <param name="name">The member name.</param>
        /// <param name="status">The member status.</param>
        [Given(@"another gossip state with member (.*) as (.*)")]
        public void GivenAnotherGossipStateWithMemberAsStatus(string name, string status)
        {
            var nodeId = this.GetOrCreateNodeId(name);
            var endpoint = new NodeEndpoint(nodeId, 9001);
            var memberStatus = Enum.Parse<MemberStatus>(status);
            var member = new Member(endpoint, memberStatus);
            var state = new GossipState();
            state.Members[nodeId] = member;
            this.context["state2"] = state;
        }

        /// <summary>
        /// Creates a gossip state with a member having specific heartbeat and status.
        /// </summary>
        /// <param name="name">The member name.</param>
        /// <param name="heartbeat">The heartbeat sequence.</param>
        /// <param name="status">The member status.</param>
        [Given(@"a gossip state with member (.*) heartbeat (\d+) status (.*)")]
        public void GivenAGossipStateWithMemberHeartbeatStatus(string name, int heartbeat, string status)
        {
            var nodeId = this.GetOrCreateNodeId(name);
            var endpoint = new NodeEndpoint(nodeId, 9000);
            var memberStatus = Enum.Parse<MemberStatus>(status);
            var member = new Member(endpoint, memberStatus)
            {
                HeartbeatSequence = heartbeat,
            };
            var state = new GossipState();
            state.Members[nodeId] = member;
            this.context["state1"] = state;
        }

        /// <summary>
        /// Creates another gossip state with a member having specific heartbeat and status.
        /// </summary>
        /// <param name="name">The member name.</param>
        /// <param name="heartbeat">The heartbeat sequence.</param>
        /// <param name="status">The member status.</param>
        [Given(@"another gossip state with member (.*) heartbeat (\d+) status (.*)")]
        public void GivenAnotherGossipStateWithMemberHeartbeatStatus(string name, int heartbeat, string status)
        {
            var nodeId = this.GetOrCreateNodeId(name);
            var endpoint = new NodeEndpoint(nodeId, 9001);
            var memberStatus = Enum.Parse<MemberStatus>(status);
            var member = new Member(endpoint, memberStatus)
            {
                HeartbeatSequence = heartbeat,
            };
            var state = new GossipState();
            state.Members[nodeId] = member;
            this.context["state2"] = state;
        }

        /// <summary>
        /// Merges the two gossip states.
        /// </summary>
        [When(@"the states are merged")]
        public void WhenTheStatesAreMerged()
        {
            var state1 = this.context.Get<GossipState>("state1");
            var state2 = this.context.Get<GossipState>("state2");
            var (merged, _) = state1.Merge(state2);
            this.context["merged"] = merged;
        }

        /// <summary>
        /// Verifies the merged state contains both members.
        /// </summary>
        [Then(@"the merged state should contain both members")]
        public void ThenTheMergedStateShouldContainBothMembers()
        {
            var merged = this.context.Get<GossipState>("merged");
            merged.Members.Count.ShouldBe(2);
        }

        /// <summary>
        /// Verifies a member has the expected status and heartbeat after merge.
        /// </summary>
        /// <param name="name">The member name.</param>
        /// <param name="status">The expected status.</param>
        /// <param name="heartbeat">The expected heartbeat.</param>
        [Then(@"member (.*) should have status (.*) and heartbeat (\d+)")]
        public void ThenMemberShouldHaveStatusAndHeartbeat(string name, string status, int heartbeat)
        {
            var merged = this.context.Get<GossipState>("merged");
            var nodeId = this.context.Get<string>($"nodeId_{name}");
            var member = merged.Members[nodeId];
            member.Status.ShouldBe(Enum.Parse<MemberStatus>(status));
            member.HeartbeatSequence.ShouldBe(heartbeat);
        }

        private string GetOrCreateNodeId(string name)
        {
            var key = $"nodeId_{name}";
            if (!this.context.ContainsKey(key))
            {
                this.context[key] = Guid.NewGuid().ToString();
            }

            return this.context.Get<string>(key);
        }
    }
}
