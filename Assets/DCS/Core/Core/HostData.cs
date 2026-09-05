namespace DynamicComponent
{
    // ============================================================
    //  HOST — OWNER OF COMPONENTS
    // ============================================================

    /// <summary>
    /// Game object (Host) — owner of a component chain.
    /// </summary>
    /// <remarks>
    /// Host is a lightweight identifier that binds components into a single entity.
    /// Contains no transform, physics, or logic data — only an identifier and a chain reference.
    ///
    /// Lifecycle:
    /// 1. Created via HostManager.CreateHost().
    /// 2. Components are added via HostChainManager.Add().
    /// 3. On destruction, HostManager.DestroyHost() is called.
    ///
    /// Memory: 16 bytes total (4 fields × 4 bytes).
    /// </remarks>
    public struct HostData
    {
        /// <summary>Unique host identifier.</summary>
        /// <remarks>
        /// Used as an index into HostManager.GlobalHosts.
        /// Does not change during the host's lifetime.
        /// </remarks>
        public int Id;

        /// <summary>Host generation — protects against stale handles.</summary>
        /// <remarks>
        /// Incremented each time the host is destroyed (HostManager.Invalidate).
        /// Allows detection of stale host references:
        /// HostManager.IsValid(host) => GlobalHosts[host.Id].Generation == host.Generation
        /// </remarks>
        public int Generation;

        /// <summary>Index of the first component in the chain (HostChainManager).</summary>
        /// <remarks>
        /// Points to the first node (ChainNode) in the host's component linked list.
        /// -1 means the chain is empty.
        /// Access the chain via HostChainManager.GetChain(host).
        /// </remarks>
        public int FirstComponent;

        /// <summary>Index of the next free host in the free list.</summary>
        /// <remarks>
        /// Used by HostManager to organize the free host pool (Free List).
        /// -1 means the end of the list.
        /// Allows reuse of host IDs without new memory allocations.
        /// </remarks>
        public int Next;
    }
}