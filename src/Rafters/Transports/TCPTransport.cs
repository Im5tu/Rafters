using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rafters.Transports
{
    // TODO :: implement event pipeline

    // Inspo: https://devblogs.microsoft.com/dotnet/system-io-pipelines-high-performance-io-in-net/
    internal class TCPTransport : TransportBase
    {
        private readonly IPEndPoint _bindingEndpoint;
        private readonly string _bindingEndpointAsString;
        private readonly INodeDiscoverer _nodeDiscoverer;
        private readonly ConcurrentDictionary<string, Socket> _knownNodes = new ConcurrentDictionary<string, Socket>();
        private bool _initialized = false;
        private Socket? _socket = null;
        private CancellationTokenSource? _cancellationTokenSource = null;
        private Task? _acceptanceLoop;
        private Task? _discoveryLoop;

        public TCPTransport(IPEndPoint bindingEndpoint, INodeDiscoverer nodeDiscoverer)
        {
            _bindingEndpointAsString = $"{bindingEndpoint.Address}:{bindingEndpoint.Port}";
            if (bindingEndpoint.AddressFamily != AddressFamily.InterNetwork && bindingEndpoint.AddressFamily != AddressFamily.InterNetworkV6)
                throw new ArgumentException($"Only InterNetwork and InterNetworkV6 address families are supported for the TCP transport. Cannot use endpoint '{_bindingEndpointAsString}' with address family '{bindingEndpoint.AddressFamily}'");

            _bindingEndpoint = bindingEndpoint;
            _nodeDiscoverer = nodeDiscoverer;
        }
                
        public override Task SendMessageAsync<T>(NodeIdentity nodeIdentity, T message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task StartAsync(IRaftNode node, CancellationToken cancellationToken)
        {
            lock (Lock)
            {
                if (_initialized)
                    return Task.CompletedTask;

                _initialized = true;
                _socket?.Dispose();
                _socket = new Socket(_bindingEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _socket.Bind(_bindingEndpoint);
                _socket.Listen(10);

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();

                // This loop ensures that we connect to other nodes, if we haven't connected already
                _discoveryLoop = DiscoverNewNodesAsync(_cancellationTokenSource.Token);

                // This loop ensures that we can process new connections
                _acceptanceLoop = AcceptNewNodesAsync(_cancellationTokenSource.Token);
            }

            return Task.CompletedTask;
        }

        private async Task DiscoverNewNodesAsync(CancellationToken cancellationToken)
        {
            var nodes = await _nodeDiscoverer.DiscoverNodesAsync(cancellationToken).ConfigureAwait(false);
            var rnd = new Random();

            while (!cancellationToken.IsCancellationRequested)
            {
                // Randomise the initial delay so that avoid some conflicts in connections
                await Task.Delay(rnd.Next(100, 1000)).ConfigureAwait(false);

                foreach (var node in nodes)
                {
                    if (string.IsNullOrWhiteSpace(node.Address) || string.Equals(node.Address, _bindingEndpointAsString, StringComparison.OrdinalIgnoreCase))
                        continue; // We are already connected, or the node is invalid

                    var splitIndex = node.Address.IndexOf(':');
                    if (splitIndex < 0)
                        continue; // There's nothing we can do without a port

                    // TODO :: DNS support if IP Address parsing fails
                    if (IPAddress.TryParse(node.Address.Substring(0, splitIndex), out var ip) && int.TryParse(node.Address.Substring(splitIndex + 1), out var port))
                    {
                        var endpoint = new IPEndPoint(ip, port);

                        var socket = _knownNodes.GetOrAdd(node.Address, address =>
                        {
                            // TODO :: Sort this dispose
#pragma warning disable IDISP001 // Dispose created. 
                            return new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
#pragma warning restore IDISP001 // Dispose created.
                        });
                        await socket.ConnectAsync(endpoint).ConfigureAwait(false);

                        // TODO :: Proper hello message
                        var str = Encoding.UTF8.GetBytes("Hello World");
                        var length = BitConverter.GetBytes(str.Length);

                        var buffer = new byte[str.Length + length.Length + 1];
                        buffer[0] = 1; // Message Version
                        Buffer.BlockCopy(length, 0, buffer, 1, length.Length);
                        Buffer.BlockCopy(str, 0, buffer, 5, str.Length);

                        await socket.SendAsync(buffer, SocketFlags.None).ConfigureAwait(false);
                    }
                }

                // TODO :: necessary? Can we move this to the event pipe?
                await Task.Delay(5000).ConfigureAwait(false);
            }
        }

        private async Task AcceptNewNodesAsync(CancellationToken cancellationToken)
        {
            var socket = _socket;
            if (socket is null)
                return;

            while (!cancellationToken.IsCancellationRequested)
            {
#pragma warning disable IDISP001 // Dispose created. - TODO : FIX
                var newlyConnectedSocket = await socket.AcceptAsync().ConfigureAwait(false);
                var remoteEndpoint = newlyConnectedSocket.RemoteEndPoint;
                if (remoteEndpoint is IPEndPoint endpoint && !_knownNodes.TryAdd($"{endpoint.Address}:{endpoint.Port}", newlyConnectedSocket))
                {
                    // TODO :: ERROR THIS LATER
                    Console.WriteLine("Conncetion already established with socket: " + $"{endpoint.Address}:{endpoint.Port}");
                    newlyConnectedSocket.Close();
                    newlyConnectedSocket.Dispose();
                    return;
                }
#pragma warning restore IDISP001 // Dispose created.
                _ = ProcessNewConnectionAsync(newlyConnectedSocket, cancellationToken);
            }
        }

        private async Task ProcessNewConnectionAsync(Socket socket, CancellationToken cancellationToken)
        {
            var remoteEndpoint = socket.RemoteEndPoint.ToString();
            Console.WriteLine("Node Connected: " + remoteEndpoint);
            using var stream = new NetworkStream(socket);
            var pipeReader = PipeReader.Create(stream); // TODO :: Investigate the options that we can pass in here

            /*
                Version 1 Datagram:

                ----------------------------------------------
                | version | length | payload                 |
                | (byte)  |  (int) |  (byte[] - length prop) |
                ----------------------------------------------
             */

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ReadResult result = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    // Should never be true, but you never know
                    if (buffer.IsEmpty)
                        continue;
                    
                    // Stop reading if there's no more data coming.
                    if (result.IsCompleted)
                        break;

                    ParseMessageFromResult(ref result, remoteEndpoint);
                    socket.Disconnect(false);
                }
            }
            catch (Exception ex)
            {
                // TODO :: log exception for the connection
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                // Mark the PipeReader as complete.
                await pipeReader.CompleteAsync().ConfigureAwait(false);
                Console.WriteLine($"[{socket.RemoteEndPoint}]: disconnected");
            }
        }

        private void ParseMessageFromResult(ref ReadResult result, string endpoint)
        {
            SequenceReader<byte> reader = new SequenceReader<byte>(result.Buffer);

            if (!reader.TryRead(out var version))
                throw new Exception("Version cannot be read from the stream. The stream may be corrupted");

            if (version != 1)
                throw new Exception("Unsupported version.");

            if (!reader.TryReadLittleEndian(out int length))
                throw new Exception("Unable to read the length from the stream");

            // Advance 1 for the version, 4 for the length
            //reader.Advance(5);

            var bytes = reader.Sequence.Slice(reader.Position, length).ToArray();
            reader.Advance(length);

            Console.WriteLine($"Received '{Encoding.UTF8.GetString(bytes)}' from '{endpoint}' (version: {version} content length: {length})");
        }

        public override Task StopAsync(IRaftNode node, CancellationToken cancellationToken)
        {
            lock (Lock)
            {
                if (!_initialized)
                    return Task.CompletedTask;
                
                CleanUp();
            }

            return Task.CompletedTask;
        }

        protected override void OnDispose()
        {
            CleanUp();
        }

        private void CleanUp()
        {
            if (_cancellationTokenSource is { })
            {
                _cancellationTokenSource.Cancel(false);
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            if (_socket is { })
            {
                // TODO :: ensure that we flush any pending messages
                // TODO :: see whether socket needs both of these
                _socket.Disconnect(true);
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Dispose();
                _socket = null;
            }

            _initialized = false;
        }
    }
}