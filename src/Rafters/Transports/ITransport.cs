using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rafters.Transports
{
    // TODO :: Implement these transport types
    //              - UDP
    //              - Pipes (IPC)

    /// <summary>
    ///     To allow us to connect to other systems
    /// </summary>
    public interface ITransport : IDisposable
    {
        /// <summary>
        ///     Want to be able to send messages to other systems
        /// </summary>
        /// <typeparam name="T">T must be serializable by Protobuf</typeparam>
        /// <param name="nodeIdentity">The destination node for the message</param>
        /// <param name="message">The message to send</param>
        /// <param name="cancellationToken">Timeout token</param>
        Task SendMessageAsync<T>(NodeIdentity nodeIdentity, T message, CancellationToken cancellationToken);

        /// <summary>
        ///     Start the underlying transport
        /// </summary>
        Task StartAsync(IRaftNode node, CancellationToken cancellationToken);

        /// <summary>
        ///     Stop the underlying transport
        /// </summary>
        Task StopAsync(IRaftNode node, CancellationToken cancellationToken);
    }
}