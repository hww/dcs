using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

namespace DynamicComponent
{
    // ============================================================
    //  CHAIN NODE — HOST COMPONENT CHAIN NODE
    // ============================================================

    /// <summary>
    /// Node in a linked list of components belonging to a single host.
    /// </summary>
    /// <remarks>
    /// Each node stores a component reference (DcsHandle), its type (TypeId),
    /// and a reference to the next node in the chain.
    ///
    /// The component chain enables:
    /// - Fast iteration over all components of a host
    /// - Finding components by type
    /// - Freeing all components when the host is destroyed
    ///
    /// Memory: 24 bytes (DcsHandle 8 bytes + 2 int × 4 bytes = 16, aligned to 24)
    /// </remarks>
    public struct ChainNode
    {
        /// <summary>Component handle.</summary>
        public Handle Component;

        /// <summary>Component type identifier (ComponentType{T}.Id).</summary>
        public int TypeId;

        /// <summary>Index of the next node in the chain.</summary>
        /// <remarks>
        /// -1 indicates the end of the chain.
        /// Used as an index into the _components array.
        /// </remarks>
        public int Next;

        /// <summary>Checks whether the node is empty.</summary>
        public bool IsNull => Component.IsNull;
    }

    // ============================================================
    //  HOST CHAIN MANAGER — COMPONENT CHAIN MANAGEMENT
    // ============================================================

    /// <summary>
    /// Manages component chains for all hosts.
    /// </summary>
    /// <remarks>
    /// Stores an array of all nodes (ChainNode) and manages linked lists of components
    /// for each host. The chain head is stored in Host.FirstComponent.
    ///
    /// Main operations:
    /// - Add: Add a component to a host's chain
    /// - Remove: Remove a component from a host's chain
    /// - Contains: Check if a component exists
    /// - GetTypedHandle: Find a component by type
    /// - FreeChain: Free all components of a host
    ///
    /// Memory: Array of MaxComponents (500,000) elements × 24 bytes = 12 MB
    /// </remarks>
    public class HostChain
    {
        /// <summary>Maximum number of concurrently existing components.</summary>
        public const int MaxComponents = 500000;

        /// <summary>Array of all chain nodes.</summary>
        private readonly ChainNode[] _components = new ChainNode[MaxComponents];

        /// <summary>Index of the first free node in the free node list.</summary>
        /// <remarks>
        /// -1 means no free nodes are available.
        /// The linked list is organized via the ChainNode.Next field.
        /// </remarks>
        private int _firstFree;

        /// <summary>Initializes the node array and free node list.</summary>
        /// <remarks>
        /// All nodes are initially free and linked via the Next field.
        /// _firstFree = 0 — the first free node.
        /// </remarks>
        public HostChain()
        {
            _firstFree = 0;

            for (int i = 0; i < MaxComponents; i++)
            {
                _components[i] = new ChainNode
                {
                    Next = i + 1,
                    Component = new Handle { Id = -1 }
                };
            }

            _components[MaxComponents - 1].Next = -1;
        }

        /// <summary>Adds a component to a host's chain.</summary>
        /// <param name="host">Host owner.</param>
        /// <param name="component">Component handle.</param>
        /// <param name="typeId">Component type identifier.</param>
        /// <remarks>
        /// Algorithm:
        /// 1. Validates the host
        /// 2. Checks that the component is not already in the chain
        /// 3. Takes the first free node
        /// 4. Fills the node with data
        /// 5. Adds the node to the head of the host's chain
        ///
        /// Complexity: O(1) + O(N) for Contains (but Contains is only called in debug)
        /// </remarks>
        public void Add(Host host, Handle component, int typeId)
        {
            if (!HostManager.IsValid(host)) return;
            if (Contains(host, component, typeId)) return;

            if (_firstFree == -1)
                throw new System.Exception("DCS Error: Out of memory for ChainNode!");

            int allocatedNodeIndex = _firstFree;
            _firstFree = _components[allocatedNodeIndex].Next;

            ref ChainNode node = ref _components[allocatedNodeIndex];
            node.Component = component;
            node.TypeId = typeId;

            ref HostData hostRef = ref HostManager.GlobalHosts[host.Id];
            node.Next = hostRef.FirstComponent;
            hostRef.FirstComponent = allocatedNodeIndex;
        }

        /// <summary>Removes a component from a host's chain.</summary>
        /// <param name="host">Host owner.</param>
        /// <param name="component">Component handle.</param>
        /// <param name="typeId">Component type identifier.</param>
        /// <remarks>
        /// Algorithm:
        /// 1. Validates the host
        /// 2. Finds the node with the given component and type
        /// 3. Unlinks the node from the linked list
        /// 4. Returns the node to the free list
        ///
        /// Complexity: O(N) where N is the number of components of the host
        /// </remarks>
        public void Remove(Host host, Handle component, int typeId)
        {
            if (!HostManager.IsValid(host)) return;

            ref HostData hostRef = ref HostManager.GlobalHosts[host.Id];
            int currentIndex = hostRef.FirstComponent;
            int previousIndex = -1;

            while (currentIndex >= 0)
            {
                ref ChainNode node = ref _components[currentIndex];

                if (node.Component.Id == component.Id && node.TypeId == typeId)
                {
                    if (previousIndex == -1)
                        hostRef.FirstComponent = node.Next;
                    else
                        _components[previousIndex].Next = node.Next;

                    node = default;
                    _components[currentIndex].Next = _firstFree;
                    _firstFree = currentIndex;
                    return;
                }

                previousIndex = currentIndex;
                currentIndex = node.Next;
            }
        }

        /// <summary>Checks whether a component exists in a host's chain.</summary>
        /// <param name="host">Host owner.</param>
        /// <param name="component">Component handle.</param>
        /// <param name="typeId">Component type identifier.</param>
        /// <returns>True if the component is found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Host host, Handle component, int typeId)
        {
            if (!HostManager.IsValid(host)) return false;

            int currentIndex = HostManager.GlobalHosts[host.Id].FirstComponent;

            while (currentIndex >= 0)
            {
                ref ChainNode node = ref _components[currentIndex];
                if (node.Component.Id == component.Id && node.TypeId == typeId)
                    return true;

                currentIndex = node.Next;
            }

            return false;
        }

        /// <summary>Finds the first component of the specified type in a host's chain.</summary>
        /// <param name="host">Host owner.</param>
        /// <param name="typeId">Component type identifier.</param>
        /// <returns>The node with the component, or default if not found.</returns>
        public ChainNode GetTypedHandle(Host host, int typeId)
        {
            if (!HostManager.IsValid(host)) return default;

            int currentIndex = HostManager.GlobalHosts[host.Id].FirstComponent;

            while (currentIndex >= 0)
            {
                ref ChainNode node = ref _components[currentIndex];
                if (node.TypeId == typeId)
                    return node;

                currentIndex = node.Next;
            }

            return default;
        }

        /// <summary>Frees all components of a host.</summary>
        /// <param name="host">Host owner.</param>
        /// <remarks>
        /// Algorithm:
        /// 1. Validates the host
        /// 2. Iterates over all nodes in the host's chain
        /// 3. For each node, calls SystemFree on the corresponding pool
        /// 4. Returns all nodes to the free list
        /// 5. Clears the host's FirstComponent
        /// 6. Invalidates the host
        ///
        /// Complexity: O(N) where N is the number of components of the host
        /// </remarks>
        public void FreeChain(Host host)
        {
            if (!HostManager.IsValid(host)) return;

            ref HostData hostRef = ref HostManager.GlobalHosts[host.Id];
            int currentIndex = hostRef.FirstComponent;

            while (currentIndex >= 0)
            {
                ref ChainNode node = ref _components[currentIndex];

                IComponentPool pool = ComponentRegistry.Pools[node.TypeId];
                pool.SystemFree(host, this, node.Component);

                int nextIndex = node.Next;

                node = default;
                _components[currentIndex].Next = _firstFree;
                _firstFree = currentIndex;

                currentIndex = nextIndex;
            }

            hostRef.FirstComponent = -1;
            HostManager.Invalidate(host);
        }
    }
}