// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TcpTransport.cs" company="MLambda">
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

namespace MLambda.Actors.Network
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;
    using MLambda.Actors.Abstraction.Core;
    using MLambda.Actors.Network.Abstraction;

    /// <summary>
    /// TCP-based transport implementation using TcpListener/TcpClient.
    /// </summary>
    public class TcpTransport : ITransport
    {
        private readonly IEventStream eventStream;
        private readonly Subject<Envelope> incomingSubject;
        private readonly ConcurrentDictionary<Guid, TcpClient> connectionPool;
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> connectionLocks;
        private readonly object listenerLock;

        private TcpListener listener;
        private CancellationTokenSource cancellation;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpTransport"/> class.
        /// </summary>
        /// <param name="localEndpoint">The local endpoint to listen on.</param>
        /// <param name="eventStream">The event stream for connection events.</param>
        public TcpTransport(NodeEndpoint localEndpoint, IEventStream eventStream)
        {
            this.LocalEndpoint = localEndpoint;
            this.eventStream = eventStream;
            this.incomingSubject = new Subject<Envelope>();
            this.connectionPool = new ConcurrentDictionary<Guid, TcpClient>();
            this.connectionLocks = new ConcurrentDictionary<Guid, SemaphoreSlim>();
            this.listenerLock = new object();
        }

        /// <inheritdoc/>
        public NodeEndpoint LocalEndpoint { get; }

        /// <inheritdoc/>
        public IObservable<Envelope> IncomingMessages => this.incomingSubject.AsObservable();

        /// <inheritdoc/>
        public IObservable<Unit> Start()
        {
            return Observable.FromAsync(async () =>
            {
                this.cancellation = new CancellationTokenSource();
                this.listener = new TcpListener(IPAddress.Parse(this.LocalEndpoint.Host), this.LocalEndpoint.Port);
                this.listener.Start();
                _ = this.AcceptConnectionsAsync(this.cancellation.Token);
                await Task.CompletedTask;
            });
        }

        /// <inheritdoc/>
        public IObservable<Unit> Stop()
        {
            return Observable.FromAsync(async () =>
            {
                this.cancellation?.Cancel();

                lock (this.listenerLock)
                {
                    this.listener?.Stop();
                }

                foreach (var kvp in this.connectionPool)
                {
                    this.CloseConnection(kvp.Key);
                }

                this.connectionPool.Clear();
                await Task.CompletedTask;
            });
        }

        /// <inheritdoc/>
        public IObservable<Unit> Send(NodeEndpoint target, Envelope envelope)
        {
            return Observable.FromAsync(async () =>
            {
                var data = EnvelopeCodec.Encode(envelope);
                var lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length));

                try
                {
                    var client = await this.GetOrCreateConnectionAsync(target);
                    var stream = client.GetStream();
                    await stream.WriteAsync(lengthPrefix, 0, 4);
                    await stream.WriteAsync(data, 0, data.Length);
                    await stream.FlushAsync();
                }
                catch (IOException)
                {
                    this.CloseConnection(target.NodeId);
                    var client = await this.GetOrCreateConnectionAsync(target);
                    var stream = client.GetStream();
                    await stream.WriteAsync(lengthPrefix, 0, 4);
                    await stream.WriteAsync(data, 0, data.Length);
                    await stream.FlushAsync();
                }
                catch (SocketException)
                {
                    this.CloseConnection(target.NodeId);
                    var client = await this.GetOrCreateConnectionAsync(target);
                    var stream = client.GetStream();
                    await stream.WriteAsync(lengthPrefix, 0, 4);
                    await stream.WriteAsync(data, 0, data.Length);
                    await stream.FlushAsync();
                }
            });
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes managed resources.
        /// </summary>
        /// <param name="disposing">True if disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed && disposing)
            {
                this.disposed = true;
                this.cancellation?.Cancel();
                this.cancellation?.Dispose();

                lock (this.listenerLock)
                {
                    this.listener?.Stop();
                }

                foreach (var kvp in this.connectionPool)
                {
                    this.CloseConnection(kvp.Key);
                }

                foreach (var kvp in this.connectionLocks)
                {
                    kvp.Value.Dispose();
                }

                this.incomingSubject.Dispose();
            }
        }

        private async Task AcceptConnectionsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client;
                    lock (this.listenerLock)
                    {
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }
                    }

                    client = await this.listener.AcceptTcpClientAsync(token);
                    _ = this.ReadConnectionAsync(client, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    break;
                }
            }
        }

        private async Task ReadConnectionAsync(TcpClient client, CancellationToken token)
        {
            NodeEndpoint remoteEndpoint = null;

            try
            {
                var stream = client.GetStream();

                while (!token.IsCancellationRequested && client.Connected)
                {
                    var lengthBuffer = new byte[4];
                    var bytesRead = await ReadExactAsync(stream, lengthBuffer, 0, 4, token);
                    if (bytesRead < 4)
                    {
                        break;
                    }

                    var length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBuffer, 0));
                    var dataBuffer = new byte[length];
                    bytesRead = await ReadExactAsync(stream, dataBuffer, 0, length, token);
                    if (bytesRead < length)
                    {
                        break;
                    }

                    var envelope = EnvelopeCodec.Decode(dataBuffer);

                    if (remoteEndpoint == null && envelope.SourceNode != null)
                    {
                        remoteEndpoint = envelope.SourceNode;
                        this.connectionPool.TryAdd(remoteEndpoint.NodeId, client);
                        this.eventStream.Publish(new ConnectionEstablished(remoteEndpoint));
                    }

                    this.incomingSubject.OnNext(envelope);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (IOException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                if (remoteEndpoint != null)
                {
                    this.connectionPool.TryRemove(remoteEndpoint.NodeId, out _);
                    this.eventStream.Publish(new ConnectionLost(remoteEndpoint));
                }

                client.Dispose();
            }
        }

        private async Task<TcpClient> GetOrCreateConnectionAsync(NodeEndpoint target)
        {
            if (this.connectionPool.TryGetValue(target.NodeId, out var existing) && IsConnected(existing))
            {
                return existing;
            }

            var connectionLock = this.connectionLocks.GetOrAdd(target.NodeId, _ => new SemaphoreSlim(1, 1));
            await connectionLock.WaitAsync();

            try
            {
                if (this.connectionPool.TryGetValue(target.NodeId, out existing) && IsConnected(existing))
                {
                    return existing;
                }

                if (existing != null)
                {
                    this.connectionPool.TryRemove(target.NodeId, out _);
                    try
                    {
                        existing.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }

                var client = new TcpClient();
                await client.ConnectAsync(target.Host, target.Port);
                this.connectionPool[target.NodeId] = client;
                this.eventStream.Publish(new ConnectionEstablished(target));

                _ = this.ReadConnectionAsync(client, this.cancellation.Token);

                return client;
            }
            finally
            {
                connectionLock.Release();
            }
        }

        private static bool IsConnected(TcpClient client)
        {
            try
            {
                return client.Connected && client.Client != null && client.Client.Connected;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        private void CloseConnection(Guid nodeId)
        {
            if (this.connectionPool.TryRemove(nodeId, out var client))
            {
                try
                {
                    client.Dispose();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        private static async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken token)
        {
            var totalRead = 0;
            while (totalRead < count)
            {
                var read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, token);
                if (read == 0)
                {
                    return totalRead;
                }

                totalRead += read;
            }

            return totalRead;
        }
    }
}
