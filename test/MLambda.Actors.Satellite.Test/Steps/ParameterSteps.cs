// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ParameterSteps.cs" company="MLambda">
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
    using System.Collections.Generic;
    using MLambda.Actors.Abstraction;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for parameter tests.
    /// </summary>
    [Binding]
    public class ParameterSteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterSteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public ParameterSteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates a parameter with an integer value.
        /// </summary>
        /// <param name="key">The parameter key.</param>
        /// <param name="value">The parameter value.</param>
        [Given(@"a parameter with key ""(.*)"" set to (\d+)")]
        public void GivenAParameterWithIntValue(string key, int value)
        {
            var parameter = new Parameter { [key] = value };
            this.context["parameter"] = parameter;
        }

        /// <summary>
        /// Creates a parameter with a string value.
        /// </summary>
        /// <param name="key">The parameter key.</param>
        /// <param name="value">The parameter value.</param>
        [Given(@"a parameter with key ""(.*)"" set to ""(.*)""")]
        public void GivenAParameterWithStringValue(string key, string value)
        {
            var parameter = new Parameter { [key] = value };
            this.context["parameter"] = parameter;
        }

        /// <summary>
        /// Creates an empty parameter.
        /// </summary>
        [Given(@"a new empty parameter")]
        public void GivenANewEmptyParameter()
        {
            this.context["parameter"] = new Parameter();
        }

        /// <summary>
        /// Creates a parameter from a dictionary.
        /// </summary>
        /// <param name="key">The dictionary key.</param>
        /// <param name="value">The dictionary value.</param>
        [Given(@"a parameter constructed from a dictionary with key ""(.*)"" and value ""(.*)""")]
        public void GivenAParameterFromDictionary(string key, string value)
        {
            var dict = new Dictionary<string, object> { { key, value } };
            this.context["parameter"] = new Parameter(dict);
        }

        /// <summary>
        /// Converts the parameter to a dictionary.
        /// </summary>
        [When(@"converted to dictionary")]
        public void WhenConvertedToDictionary()
        {
            var parameter = this.context.Get<Parameter>("parameter");
            this.context["dictionary"] = parameter.ToDictionary();
        }

        /// <summary>
        /// Verifies getting an integer value by key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="expected">The expected value.</param>
        [Then(@"getting key ""(.*)"" should return (\d+)")]
        public void ThenGettingKeyShouldReturnInt(string key, int expected)
        {
            var parameter = this.context.Get<Parameter>("parameter");
            ((int)parameter[key]).ShouldBe(expected);
        }

        /// <summary>
        /// Verifies getting a string value by key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="expected">The expected value.</param>
        [Then(@"getting key ""(.*)"" should return ""(.*)""")]
        public void ThenGettingKeyShouldReturnString(string key, string expected)
        {
            var parameter = this.context.Get<Parameter>("parameter");
            parameter[key].ToString().ShouldBe(expected);
        }

        /// <summary>
        /// Verifies the dictionary contains a key-value pair.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The expected value.</param>
        [Then(@"the dictionary should contain key ""(.*)"" with value ""(.*)""")]
        public void ThenTheDictionaryShouldContain(string key, string value)
        {
            var dict = this.context.Get<Dictionary<string, object>>("dictionary");
            dict.ShouldContainKey(key);
            dict[key].ToString().ShouldBe(value);
        }

        /// <summary>
        /// Verifies the parameter is empty.
        /// </summary>
        [Then(@"the parameter should be empty")]
        public void ThenTheParameterShouldBeEmpty()
        {
            var parameter = this.context.Get<Parameter>("parameter");
            parameter.IsEmpty.ShouldBeTrue();
        }

        /// <summary>
        /// Verifies the parameter is not empty.
        /// </summary>
        [Then(@"the parameter should not be empty")]
        public void ThenTheParameterShouldNotBeEmpty()
        {
            var parameter = this.context.Get<Parameter>("parameter");
            parameter.IsEmpty.ShouldBeFalse();
        }
    }
}
