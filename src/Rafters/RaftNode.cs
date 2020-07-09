using System;
using System.Threading;
using System.Threading.Tasks;
using Rafters.Elections;
using Rafters.Transports;

namespace Rafters
{
    internal sealed class RaftNode : IRaftNode, IAsyncDisposable
    {
        private readonly ITransport _transport;
        private IElectionTimer? _electionTimer;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task? _electionTask;

        public string ID { get; } = Guid.NewGuid().ToString("N");
        
        public RaftNode(ITransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }
        
        /// <inheritDoc />
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _transport.StartAsync(this, cancellationToken).ConfigureAwait(false);
            _electionTimer ??= new ElectionTimer();
            _electionTask ??= StartElectionMonitorAsync(_cancellationTokenSource.Token);
        }

        /// <inheritDoc />
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _cancellationTokenSource.Cancel(false);
            _electionTimer?.Dispose();
            _electionTimer = null;
            await _transport.StopAsync(this, cancellationToken).ConfigureAwait(false);
        }

        private async Task StartElectionMonitorAsync(CancellationToken cancellationToken)
        {
            if (_electionTimer is null)
                return;
            
            cancellationToken.ThrowIfCancellationRequested();
            
            while (!cancellationToken.IsCancellationRequested)
            {
                await _electionTimer.WaitForNewElectionTermAsync(cancellationToken).ConfigureAwait(false);
                //Console.WriteLine(_nodeId + ": New Election Term Started");
            }
        }

        public ValueTask DisposeAsync()
        {
            _cancellationTokenSource.Cancel(false);
            _cancellationTokenSource.Dispose();
            _electionTimer?.Dispose();
            _electionTimer = null;
            return new ValueTask(StopAsync(default));
        }
    }
}