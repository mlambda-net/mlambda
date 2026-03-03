// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RouteTemplateSteps.cs" company="MLambda">
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

namespace MLambda.Actors.Satellite.Test.Steps
{
    using System;
    using System.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Routing;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for route template tests.
    /// </summary>
    [Binding]
    public class RouteTemplateSteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteTemplateSteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public RouteTemplateSteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates a route template.
        /// </summary>
        /// <param name="template">The template string.</param>
        [Given(@"a route template ""(.*)""")]
        public void GivenARouteTemplate(string template)
        {
            this.context["template"] = new RouteTemplate(template);
        }

        /// <summary>
        /// Resolves the template with a single parameter.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="paramValue">The parameter value.</param>
        [When(@"resolved with parameter (.*) equal to (.*)")]
        public void WhenResolvedWithParameter(string paramName, int paramValue)
        {
            var template = this.context.Get<RouteTemplate>("template");
            var parameters = new Parameter { [paramName] = paramValue };
            var resolved = template.Resolve(parameters);
            this.context["resolved"] = resolved;
        }

        /// <summary>
        /// Resolves the template with two string parameters.
        /// </summary>
        /// <param name="name1">First parameter name.</param>
        /// <param name="value1">First parameter value.</param>
        /// <param name="name2">Second parameter name.</param>
        /// <param name="value2">Second parameter value.</param>
        [When(@"resolved with parameters (.*) equal to ""(.*)"" and (.*) equal to ""(.*)""")]
        public void WhenResolvedWithMultipleParameters(string name1, string value1, string name2, string value2)
        {
            var template = this.context.Get<RouteTemplate>("template");
            var parameters = new Parameter { [name1] = value1, [name2] = value2 };
            var resolved = template.Resolve(parameters);
            this.context["resolved"] = resolved;
        }

        /// <summary>
        /// Attempts to resolve with empty parameters and captures the exception.
        /// </summary>
        [When(@"resolved with empty parameters")]
        public void WhenResolvedWithEmptyParameters()
        {
            var template = this.context.Get<RouteTemplate>("template");
            try
            {
                template.Resolve(new Parameter());
            }
            catch (Exception ex)
            {
                this.context["exception"] = ex;
            }
        }

        /// <summary>
        /// Matches a resolved route against the template.
        /// </summary>
        /// <param name="resolvedRoute">The resolved route to match.</param>
        [When(@"matching against ""(.*)""")]
        public void WhenMatchingAgainst(string resolvedRoute)
        {
            var template = this.context.Get<RouteTemplate>("template");
            var success = template.TryMatch(resolvedRoute, out var parameters);
            this.context["matchSuccess"] = success;
            if (parameters != null)
            {
                this.context["matchedParams"] = parameters;
            }
        }

        /// <summary>
        /// Verifies the template is not parameterized.
        /// </summary>
        [Then(@"the template should not be parameterized")]
        public void ThenTheTemplateShouldNotBeParameterized()
        {
            var template = this.context.Get<RouteTemplate>("template");
            template.IsParameterized.ShouldBeFalse();
        }

        /// <summary>
        /// Verifies the template is parameterized.
        /// </summary>
        [Then(@"the template should be parameterized")]
        public void ThenTheTemplateShouldBeParameterized()
        {
            var template = this.context.Get<RouteTemplate>("template");
            template.IsParameterized.ShouldBeTrue();
        }

        /// <summary>
        /// Verifies the parameter names.
        /// </summary>
        /// <param name="expectedNames">Comma-separated list of expected parameter names.</param>
        [Then(@"the parameter names should be ""(.*)""")]
        public void ThenTheParameterNamesShouldBe(string expectedNames)
        {
            var template = this.context.Get<RouteTemplate>("template");
            var expected = expectedNames.Split(',').Select(n => n.Trim()).ToArray();
            template.ParameterNames.ShouldBe(expected);
        }

        /// <summary>
        /// Verifies the resolved route.
        /// </summary>
        /// <param name="expected">The expected resolved route.</param>
        [Then(@"the resolved route should be ""(.*)""")]
        public void ThenTheResolvedRouteShouldBe(string expected)
        {
            var resolved = this.context.Get<string>("resolved");
            resolved.ShouldBe(expected);
        }

        /// <summary>
        /// Verifies an ArgumentException was thrown.
        /// </summary>
        [Then(@"an ArgumentException should be thrown")]
        public void ThenAnArgumentExceptionShouldBeThrown()
        {
            this.context.ContainsKey("exception").ShouldBeTrue();
            this.context.Get<Exception>("exception").ShouldBeOfType<ArgumentException>();
        }

        /// <summary>
        /// Verifies the match succeeded.
        /// </summary>
        [Then(@"the match should succeed")]
        public void ThenTheMatchShouldSucceed()
        {
            this.context.Get<bool>("matchSuccess").ShouldBeTrue();
        }

        /// <summary>
        /// Verifies the match failed.
        /// </summary>
        [Then(@"the match should fail")]
        public void ThenTheMatchShouldFail()
        {
            this.context.Get<bool>("matchSuccess").ShouldBeFalse();
        }

        /// <summary>
        /// Verifies an extracted parameter value.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="expectedValue">The expected value.</param>
        [Then(@"the extracted parameter (.*) should be ""(.*)""")]
        public void ThenTheExtractedParameterShouldBe(string paramName, string expectedValue)
        {
            var matchedParams = this.context.Get<Parameter>("matchedParams");
            matchedParams[paramName].ToString().ShouldBe(expectedValue);
        }

        /// <summary>
        /// Verifies the base route.
        /// </summary>
        /// <param name="expected">The expected base route.</param>
        [Then(@"the base route should be ""(.*)""")]
        public void ThenTheBaseRouteShouldBe(string expected)
        {
            var template = this.context.Get<RouteTemplate>("template");
            template.GetBaseRoute().ShouldBe(expected);
        }
    }
}
