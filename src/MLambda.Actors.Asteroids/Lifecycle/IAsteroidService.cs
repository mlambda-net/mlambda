// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IAsteroidService.cs" company="MLambda">
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

namespace MLambda.Actors.Asteroids.Lifecycle
{
    using System;

    /// <summary>
    /// Lifecycle manager for asteroid nodes. Handles registration with
    /// cluster nodes, heartbeat sending, and graceful shutdown.
    /// </summary>
    public interface IAsteroidService : IDisposable
    {
        /// <summary>
        /// Starts the asteroid service: registers with cluster nodes
        /// and starts periodic heartbeats.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the asteroid service: stops heartbeat and deregisters
        /// from cluster nodes.
        /// </summary>
        void Stop();
    }
}
