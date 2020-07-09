using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Rafters.Elections
{
    // Timer per stream that randomises an election time between 150ms and 300ms
    // Once the timer fires, we create a new election term
    // We vote for ourselves
    // Request votes from others
    // If the receiving node hasn't voted yet in this term then it votes for the candidate - then resets the election timeout
    // Once a candidate has a majority of votes it becomes leader.
    internal sealed class ElectionTimer : IElectionTimer, IDisposable
    {
        // According to http://thesecretlivesofdata.com/raft/#election these are the minimum & maximum times of the election cycle
        private const int MinimumElectionTimeoutInMS = 150;
        private const int MaximumElectionTimeoutInMS = 300;
        
        private Random _random = new Random();
        private TaskCompletionSource<bool> _resetTimer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _disposed = false;

        internal Task CompletionTask => _resetTimer.Task;

        /// <inheritDoc />
        public void Reset()
        {
            Interlocked.Exchange(ref _resetTimer, new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult(true);
        }
        
        /// <inheritDoc />
        public async Task WaitForNewElectionTermAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested && !_disposed)
            {
                try
                {
                    // When we reach the timeout, we need to trigger a new election
                    var delay = _random.Next(MinimumElectionTimeoutInMS, MaximumElectionTimeoutInMS);
                    Debug.WriteLine("Next election in " + delay + "ms");
                    var electionTimeoutTask = Task.Delay(TimeSpan.FromMilliseconds(delay), cancellationToken);
                    var completedTask = await Task.WhenAny(electionTimeoutTask, _resetTimer.Task).ConfigureAwait(false);

                    if (!_disposed)
                    {
                        if (completedTask == electionTimeoutTask)
                        {
                            Debug.WriteLine("Completed task is election task");
                            return; // We need to exit here so that we complete the task
                        }
                        else
                        {
                            Debug.WriteLine("Completed task is resetable task");
                        }
                    }
                }
                catch (Exception) when (!cancellationToken.IsCancellationRequested)
                {
                    // TODO :: Log the exception
                }
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}