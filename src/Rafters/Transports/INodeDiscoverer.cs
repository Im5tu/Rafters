using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rafters.Transports
{
    /// <summary>
    ///     Allow us to find other nodes from different sources, eg: fixed list, dns etc
    /// </summary>
    public interface INodeDiscoverer
    {
        // TODO :: Add the following providers:
        //              - DNS
        //              - Docker
        //              - k8s
        //              - AWS ECS
        Task<IEnumerable<NodeIdentity>> DiscoverNodesAsync(CancellationToken cancellationToken);
    }
}
