using System.Threading;
using System.Threading.Tasks;

namespace Rafters
{
    /// <summary>
    ///    Manages multiple Raft streams within a system
    /// </summary>
    public interface IRaftNode
    {
        /// <summary>
        ///     The ID of the node
        /// </summary>
        string ID { get; }
        
        /// <summary>
        ///     Ensures that we start the election timer
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        ///     Ensures that we stop the election timer
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}