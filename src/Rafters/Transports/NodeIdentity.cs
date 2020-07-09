namespace Rafters.Transports
{
    /// <summary>
    ///     Identifies a node in the system
    /// </summary>
    public class NodeIdentity
    {
        /// <summary>
        ///     The physical location of the node
        /// </summary>
        public string? Address { get; set; }

        // TODO :: Support node identity certificates
    }
}