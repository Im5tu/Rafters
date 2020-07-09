using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rafters.Transports
{
    internal sealed class FixedListNodeDiscoverer : INodeDiscoverer
    {
        /// <inheritdoc/>
        public Task<IEnumerable<NodeIdentity>> DiscoverNodesAsync(CancellationToken cancellationToken)
        {
            // TODO :: Take this from config
            return Task.FromResult<IEnumerable<NodeIdentity>>(new List<NodeIdentity>
            {
                new NodeIdentity { Address = "127.0.0.1:6501" },
                new NodeIdentity { Address = "127.0.0.1:6502" },
                new NodeIdentity { Address = "127.0.0.1:6503" },
                new NodeIdentity { Address = "127.0.0.1:6504" },
                new NodeIdentity { Address = "127.0.0.1:6505" }
            }); 
        }
    }
}