namespace DCS.Core
{
    /// <summary>
    /// The object can be associated with a Host
    /// </summary>
    public interface IHostReference
    {
        Host Host { get; }
        void LinkToHost(Host host);
        void UnlinkFromHost();

        /// <summary>
        /// True if the underlying object is still alive and usable.
        /// Core does not know what "alive" means for a concrete implementation;
        /// MonoBehaviour actors return gameObject != null, plain objects return true.
        /// </summary>
        bool IsAlive { get; }
    }
}