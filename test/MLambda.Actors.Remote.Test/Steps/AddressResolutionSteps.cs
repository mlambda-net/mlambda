// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AddressResolutionSteps.cs" company="MLambda">
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

namespace MLambda.Actors.Remote.Test.Steps
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Core;
    using MLambda.Actors.Network;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Remote.Abstraction;
    using Moq;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for address resolution tests.
    /// </summary>
    [Binding]
    public class AddressResolutionSteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddressResolutionSteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public AddressResolutionSteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates an address resolver with an empty registry.
        /// </summary>
        [Given(@"an address resolver with an empty registry")]
        public void GivenAnAddressResolverWithEmptyRegistry()
        {
            var resolver = this.CreateResolver(new ActorRegistry());
            this.context["resolver"] = resolver;
        }

        /// <summary>
        /// Creates an address resolver with a registered remote actor.
        /// </summary>
        [Given(@"an address resolver with actor X registered on a remote node")]
        public void GivenAnAddressResolverWithActorXRegistered()
        {
            var actorId = Guid.NewGuid();
            var remoteNode = new NodeEndpoint("127.0.0.1", 9001);
            var registry = new ActorRegistry();
            registry.Register(actorId, remoteNode);

            var resolver = this.CreateResolver(registry);
            this.context["resolver"] = resolver;
            this.context["actorX"] = actorId;
        }

        /// <summary>
        /// Resolves an unknown actor id.
        /// </summary>
        [When(@"resolving an unknown actor id")]
        public void WhenResolvingAnUnknownActorId()
        {
            var resolver = this.context.Get<AddressResolver>("resolver");
            var result = resolver.Resolve(Guid.NewGuid());
            this.context["resolved"] = result as object ?? "null";
        }

        /// <summary>
        /// Resolves actor X.
        /// </summary>
        [When(@"resolving actor X")]
        public void WhenResolvingActorX()
        {
            var resolver = this.context.Get<AddressResolver>("resolver");
            var actorId = this.context.Get<Guid>("actorX");
            var result = resolver.Resolve(actorId);
            this.context["resolved"] = result as object ?? "null";
        }

        /// <summary>
        /// Verifies the resolved address is null.
        /// </summary>
        [Then(@"the resolved address should be null")]
        public void ThenTheResolvedAddressShouldBeNull()
        {
            var result = this.context["resolved"];
            result.ShouldBe("null");
        }

        /// <summary>
        /// Verifies the resolved address is a remote address.
        /// </summary>
        [Then(@"the resolved address should be a remote address")]
        public void ThenTheResolvedAddressShouldBeRemote()
        {
            var result = this.context["resolved"];
            result.ShouldBeOfType<RemoteAddress>();
        }

        private AddressResolver CreateResolver(IActorRegistry registry)
        {
            var localNode = new NodeEndpoint("127.0.0.1", 9000);
            var serializer = new JsonMessageSerializer();
            var pendingRequests = new ConcurrentDictionary<Guid, TaskCompletionSource<object>>();

            var mockBucket = new Mock<IBucket>();
            mockBucket.Setup(b => b.Filter(It.IsAny<Func<IProcess, bool>>()))
                .Returns(new List<IProcess>());

            var mockTransport = new Mock<ITransport>();
            mockTransport.Setup(t => t.LocalEndpoint).Returns(localNode);

            return new AddressResolver(
                mockBucket.Object, registry, mockTransport.Object, serializer, pendingRequests);
        }
    }
}
