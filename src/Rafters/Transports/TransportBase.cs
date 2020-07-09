using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rafters.Transports
{
#pragma warning disable IDISP025 // Class with no virtual dispose method should be sealed.
    internal abstract class TransportBase : ITransport
#pragma warning restore IDISP025 // Class with no virtual dispose method should be sealed.
    {
        private bool _disposed = false;
        protected object Lock { get; } = new object();

        public void Dispose()
        {
            lock (Lock)
            {
                if (_disposed)
                    return;

                _disposed = true;
                OnDispose();
            }
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        protected virtual void OnDispose()
        { }

        public abstract Task SendMessageAsync<T>(NodeIdentity nodeIdentity, T message, CancellationToken cancellationToken);
        public abstract Task StartAsync(IRaftNode node, CancellationToken cancellationToken);
        public abstract Task StopAsync(IRaftNode node, CancellationToken cancellationToken);
    }


}