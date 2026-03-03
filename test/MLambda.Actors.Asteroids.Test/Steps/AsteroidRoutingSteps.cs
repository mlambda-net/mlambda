// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AsteroidRoutingSteps.cs" company="MLambda">
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

namespace MLambda.Actors.Asteroids.Test.Steps
{
    using System;
    using System.Collections.Concurrent;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Cluster.Abstraction;
    using MLambda.Actors.Network;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Satellite.Abstraction;
    using Moq;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for asteroid route address tests.
    /// </summary>
    [Binding]
    public class AsteroidRoutingSteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsteroidRoutingSteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public AsteroidRoutingSteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates an asteroid route address with a mock dispatcher.
        /// </summary>
        /// <param name="route">The route name or template.</param>
        [Given(@"an asteroid route address for route ""(.*)""")]
        public void GivenAnAsteroidRouteAddressForRoute(string route)
        {
            var localEndpoint = new NodeEndpoint("127.0.0.1", 9000);
            var serializer = new JsonMessageSerializer();
            var pendingRequests = new ConcurrentDictionary<Guid, TaskCompletionSource<object>>();
            var capturedMessages = new ConcurrentBag<DispatchWork>();

            var mockDispatcherAddress = new Mock<IAddress>();
            mockDispatcherAddress.Setup(a => a.Send(It.IsAny<DispatchWork>()))
                .Callback<DispatchWork>(msg => capturedMessages.Add(msg))
                .Returns(Observable.Return(Unit.Default));

            var mockResolver = new Mock<IActorResolver>();
            mockResolver.Setup(r => r.Resolve("dispatcher"))
                .Returns(mockDispatcherAddress.Object);

            var address = new AsteroidRouteAddress(
                route, mockResolver.Object, serializer, localEndpoint, pendingRequests);

            this.context["address"] = address;
            this.context["capturedMessages"] = capturedMessages;
            this.context["pendingRequests"] = pendingRequests;
        }

        /// <summary>
        /// Sends a tell message via the asteroid address.
        /// </summary>
        [When(@"a tell message is sent via the asteroid address")]
        public void WhenATellMessageIsSent()
        {
            var address = this.context.Get<AsteroidRouteAddress>("address");
            address.Send("hello").Wait();
        }

        /// <summary>
        /// Initiates an ask message via the asteroid address.
        /// The ask will not complete (no response), but the DispatchWork is captured.
        /// </summary>
        [When(@"an ask message is initiated via the asteroid address")]
        public void WhenAnAskMessageIsInitiated()
        {
            var address = this.context.Get<AsteroidRouteAddress>("address");

            // Subscribe to start the ask; the DispatchWork is sent to the dispatcher
            // inside the observable. We don't await the result since no response will arrive.
            address.Send<string, string>("hello").Subscribe(_ => { }, ex => { });

            // Give a brief moment for the async dispatch to complete.
            System.Threading.Thread.Sleep(100);
        }

        /// <summary>
        /// Sends a tell message with a route parameter.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="paramValue">The parameter value.</param>
        [When(@"a tell message is sent with parameter (.*) equal to (.*)")]
        public void WhenATellMessageIsSentWithParameter(string paramName, int paramValue)
        {
            var address = this.context.Get<AsteroidRouteAddress>("address");
            var parameters = new Parameter { [paramName] = paramValue };
            address.Send("hello", parameters).Wait();
        }

        /// <summary>
        /// Sends an ask message with a route parameter.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="paramValue">The parameter value.</param>
        [When(@"an ask message is sent with parameter (.*) equal to (.*)")]
        public void WhenAnAskMessageIsSentWithParameter(string paramName, int paramValue)
        {
            var address = this.context.Get<AsteroidRouteAddress>("address");
            var parameters = new Parameter { [paramName] = paramValue };

            // Subscribe to start the ask; the DispatchWork is sent to the dispatcher
            // inside the observable. We don't await the result since no response will arrive.
            address.Send<string, string>("hello", parameters).Subscribe(_ => { }, ex => { });

            // Give a brief moment for the async dispatch to complete.
            System.Threading.Thread.Sleep(100);
        }

        /// <summary>
        /// Verifies a DispatchWork was sent to the dispatcher.
        /// </summary>
        [Then(@"a DispatchWork should have been sent to the dispatcher")]
        public void ThenADispatchWorkShouldHaveBeenSent()
        {
            var captured = this.context.Get<ConcurrentBag<DispatchWork>>("capturedMessages");
            captured.Count.ShouldBeGreaterThan(0);
        }

        /// <summary>
        /// Verifies the DispatchWork target route.
        /// </summary>
        /// <param name="expectedRoute">The expected route.</param>
        [Then(@"the DispatchWork target route should be ""(.*)""")]
        public void ThenTheDispatchWorkTargetRouteShouldBe(string expectedRoute)
        {
            var captured = this.context.Get<ConcurrentBag<DispatchWork>>("capturedMessages");
            captured.TryPeek(out var msg).ShouldBeTrue();
            msg.TargetRoute.ShouldBe(expectedRoute);
        }

        /// <summary>
        /// Verifies the DispatchWork IsAsk flag.
        /// </summary>
        /// <param name="expected">The expected IsAsk value.</param>
        [Then(@"the DispatchWork IsAsk should be (.*)")]
        public void ThenTheDispatchWorkIsAskShouldBe(bool expected)
        {
            var captured = this.context.Get<ConcurrentBag<DispatchWork>>("capturedMessages");
            captured.TryPeek(out var msg).ShouldBeTrue();
            msg.IsAsk.ShouldBe(expected);
        }
    }
}
