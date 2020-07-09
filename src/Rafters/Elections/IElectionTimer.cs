using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rafters.Elections
{
    /// <summary>
    ///     Manages the timeout for an election term
    /// </summary>
    public interface IElectionTimer : IDisposable
    {
        /// <summary>
        ///     Resets the election timer to a new period so that we don't trigger a new election.
        /// </summary>
        void Reset();
        
        /// <summary>
        ///     Completes the task when we should trigger a new election cycle
        /// </summary>
        /// <remarks>
        ///    On the completion of the task, we need to transition into the candidate state and request votes from the other connected nodes
        /// </remarks>
        Task WaitForNewElectionTermAsync(CancellationToken cancellationToken = default);
    }
}