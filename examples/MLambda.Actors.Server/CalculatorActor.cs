// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatorActor.cs" company="MLambda">
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

namespace MLambda.Actors.Server
{
    using System;
    using System.Reactive.Linq;
    using MLambda.Actors.Abstraction;
    using MLambda.Actors.Abstraction.Annotation;

    /// <summary>
    /// A calculator actor that performs arithmetic operations.
    /// </summary>
    [Route("calculator")]
    public class CalculatorActor : Actor
    {
        /// <inheritdoc/>
        protected override Behavior Receive(object data) =>
            data switch
            {
                CalculateRequest request => Actor.Behavior(this.Calculate, request),
                _ => Actor.Ignore,
            };

        private IObservable<double> Calculate(CalculateRequest request)
        {
            var result = request.Operation switch
            {
                "add" => request.A + request.B,
                "subtract" => request.A - request.B,
                "multiply" => request.A * request.B,
                "divide" => request.B != 0 ? request.A / request.B : double.NaN,
                _ => throw new InvalidOperationException($"Unknown operation: {request.Operation}"),
            };

            Console.WriteLine($"[Calculator] {request.A} {request.Operation} {request.B} = {result}");
            return Observable.Return(result);
        }
    }
}
