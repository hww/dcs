using System.Runtime.CompilerServices;

namespace DynamicComponent
{
    // ============================================================
    //  TYPE CHAIN NODE — SUBSCRIPTION LINK FOR EVENT TYPES
    // ============================================================

    /// <summary>
    /// Node linking a subscription to a specific event type chain.
    /// </summary>
    /// <remarks>
    /// Each node represents a subscription to a specific event type.
    /// The chain allows fast iteration over all subscriptions
    /// that are interested in a particular event type.
    ///
    /// Memory: 24 bytes (DcsHandle 8 bytes + 2 int × 4 bytes = 16, aligned to 24)
    /// </remarks>
    public struct TypeChainNode
    {
        /// <summary>Handle to the subscription structure in SubscriptionManager.</summary>
        public Handle SubscriptionHandle;

        /// <summary>TypeId of the Process-State machine pool (receiver).</summary>
        public int ProcessTypeId;

        /// <summary>Index of the next subscription in this type chain.</summary>
        /// <remarks>-1 indicates the end of the chain.</remarks>
        public int Next;
    }

    // ============================================================
    //  TYPE CHAIN MANAGER — EVENT SUBSCRIPTION CHAINS
    // ============================================================

    /// <summary>
    /// Manages subscription chains organized by event type.
    /// </summary>
    /// <remarks>
    /// For each event type, maintains a linked list of subscriptions
    /// that want to receive events of that type.
    ///
    /// Main operations:
    /// - Add: Link a subscription to an event type chain
    /// - Remove: Unlink a subscription from an event type chain
    /// - GetTypeChainHead: Get the first node of a type chain
    ///
    /// This enables O(1) access to all subscribers of a specific event type
    /// during event polling.
    ///
    /// Memory: Array of MaxTypeNodes (100,000) × 24 bytes = 2.4 MB
    /// </remarks>
    public class TypeChain
    {
        /// <summary>Maximum number of subscription nodes.</summary>
        public const int MaxTypeNodes = 100000;

        /// <summary>Array of all type chain nodes.</summary>
        private readonly TypeChainNode[] _nodes = new TypeChainNode[MaxTypeNodes];

        /// <summary>Head of each type chain (indexed by event TypeId).</summary>
        /// <remarks>-1 means no subscriptions for this event type.</remarks>
        private readonly int[] _typeChains = new int[ComponentRegistry.MaxComponentTypes];

        /// <summary>Index of the first free node in the free list.</summary>
        private int _firstFree;

        /// <summary>Initializes the node array and type chains.</summary>
        public TypeChain()
        {
            _firstFree = 0;

            // Initialize free list
            for (int i = 0; i < MaxTypeNodes; i++)
            {
                _nodes[i] = new TypeChainNode { Next = i + 1 };
                _nodes[i].SubscriptionHandle.Id = ushort.MaxValue;
            }
            _nodes[MaxTypeNodes - 1].Next = -1;

            // Initialize all type chains as empty
            for (int i = 0; i < _typeChains.Length; i++)
                _typeChains[i] = -1;
        }

        /// <summary>
        /// Links a subscription into the chain for a specific event type.
        /// </summary>
        /// <param name="eventTypeId">Event type identifier (ComponentType{TEvent}.Id).</param>
        /// <param name="subHandle">Handle to the subscription.</param>
        /// <param name="processTypeId">TypeId of the receiver process pool.</param>
        /// <exception cref="System.Exception">If the node limit is exceeded.</exception>
        /// <remarks>
        /// Algorithm:
        /// 1. Takes the first free node from the free list
        /// 2. Fills the node with subscription data
        /// 3. Inserts the node at the head of the event type chain
        ///
        /// Complexity: O(1)
        /// </remarks>
        public void Add(int eventTypeId, Handle subHandle, int processTypeId)
        {
            if (_firstFree == -1)
                throw new System.Exception("DCS Error: Out of memory in TypeChainManager!");

            int allocatedNodeIndex = _firstFree;
            _firstFree = _nodes[allocatedNodeIndex].Next;

            ref TypeChainNode node = ref _nodes[allocatedNodeIndex];
            node.SubscriptionHandle = subHandle;
            node.ProcessTypeId = processTypeId;

            // Insert at head
            node.Next = _typeChains[eventTypeId];
            _typeChains[eventTypeId] = allocatedNodeIndex;
        }

        /// <summary>
        /// Removes a subscription from the event type chain.
        /// </summary>
        /// <param name="eventTypeId">Event type identifier.</param>
        /// <param name="subHandle">Handle to the subscription to remove.</param>
        /// <remarks>
        /// Algorithm:
        /// 1. Finds the node with the matching subscription handle
        /// 2. Unlinks it from the chain
        /// 3. Returns the node to the free list
        ///
        /// Complexity: O(N) where N is the number of subscriptions for this event type
        /// </remarks>
        public void Remove(int eventTypeId, Handle subHandle)
        {
            int currentIndex = _typeChains[eventTypeId];
            int previousIndex = -1;

            while (currentIndex >= 0)
            {
                ref TypeChainNode node = ref _nodes[currentIndex];

                if (node.SubscriptionHandle.Id == subHandle.Id)
                {
                    // Unlink the node
                    if (previousIndex == -1)
                        _typeChains[eventTypeId] = node.Next;
                    else
                        _nodes[previousIndex].Next = node.Next;

                    // Return to free list
                    node = default;
                    _nodes[currentIndex].Next = _firstFree;
                    _firstFree = currentIndex;
                    return;
                }

                previousIndex = currentIndex;
                currentIndex = node.Next;
            }
        }

        /// <summary>
        /// Gets the head of the chain for a specific event type.
        /// </summary>
        /// <param name="eventTypeId">Event type identifier.</param>
        /// <returns>Index of the first node, or -1 if the chain is empty.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetTypeChainHead(int eventTypeId)
        {
            return _typeChains[eventTypeId];
        }

        /// <summary>
        /// Gets a reference to a type chain node by index.
        /// </summary>
        /// <param name="index">Node index.</param>
        /// <returns>Reference to the node.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TypeChainNode GetNode(int index)
        {
            return ref _nodes[index];
        }
    }
}