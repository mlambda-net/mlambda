// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActorTracing.cs" company="MLambda">
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

namespace MLambda.Actors.Monitoring
{
    using System.Diagnostics;

    /// <summary>
    /// Provides a shared <see cref="ActivitySource"/> for distributed tracing
    /// of actor message processing via OpenTelemetry.
    /// </summary>
    public static class ActorTracing
    {
        /// <summary>
        /// The activity source name used for OpenTelemetry SDK registration.
        /// </summary>
        public const string SourceName = "MLambda.Actors";

        /// <summary>
        /// The shared activity source instance for creating trace spans.
        /// </summary>
        public static readonly ActivitySource Source = new ActivitySource(SourceName, "1.0.0");
    }
}
