using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Rafters.Transports;

namespace Rafters.Samples.MultiNodeInMemory
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var mre = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, args) => mre.Set();

            var nodeDiscoverer = new FixedListNodeDiscoverer();

            IRaftNode node1 = new RaftNode(new TCPTransport(new IPEndPoint(IPAddress.Loopback, 6501), nodeDiscoverer));
            IRaftNode node2 = new RaftNode(new TCPTransport(new IPEndPoint(IPAddress.Loopback, 6502), nodeDiscoverer));
            IRaftNode node3 = new RaftNode(new TCPTransport(new IPEndPoint(IPAddress.Loopback, 6503), nodeDiscoverer));
            IRaftNode node4 = new RaftNode(new TCPTransport(new IPEndPoint(IPAddress.Loopback, 6504), nodeDiscoverer));
            IRaftNode node5 = new RaftNode(new TCPTransport(new IPEndPoint(IPAddress.Loopback, 6505), nodeDiscoverer));

            await Task.WhenAll(node1.StartAsync(), node2.StartAsync(), node3.StartAsync(), node4.StartAsync(), node5.StartAsync());
            
            mre.WaitOne();
            
            await Task.WhenAll(node1.StopAsync(), node2.StopAsync(), node3.StopAsync(), node4.StopAsync(), node5.StopAsync());
        }
    }
}