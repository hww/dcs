using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;
using DynamicComponent;

namespace DynamicComponent
{
    // ============================================================
    //  ROSTER ITEM — ROSTER SLOT
    // ============================================================

    /// <summary>
    /// A slot in the Roster, linking handles to component data and owners.
    /// </summary>
    /// <remarks>
    /// Each RosterItem represents a stable slot that maps:
    /// - DcsHandle (Id + Generation) → Component data (Index)
    /// - Component owner (Host)
    /// - Free list link (Next)
    ///
    /// The Roster is the "sparse array" that provides stable handles
    /// while the component data (Dense Array) can be compacted via Swap-Back.
    ///
    /// Memory: 16 bytes (4 fields × 4 bytes)
    /// </remarks>
    public struct RosterItem
    {
        /// <summary>Index into the dense component array (DenseIndex).</summary>
        public int Index;

        /// <summary>Generation for handle validation. Incremented on each free.</summary>
        public int Generation;

        /// <summary>Host that owns this component.</summary>
        public Host Host;

        /// <summary>Next free slot index (for free list) or next chain node.</summary>
        public int Next;
    }

    // ============================================================
    //  COMPONENT MANAGER — DENSE POOL WITH SPARSE ROSTER
    // ============================================================

    /// <summary>
    /// High-performance component pool with dense array storage and sparse roster handles.
    /// </summary>
    /// <typeparam name="T">Component type (must be a struct implementing IDcsComponent).</typeparam>
    /// <remarks>
    /// Architecture:
    /// - Dense Array: Contiguous storage of active components (cache-friendly iteration)
    /// - Sparse Roster: Stable handles with generation (safe references)
    /// - Swap-Back: O(1) removal with compaction
    /// - Free List: Reuses roster slots without allocation
    ///
    /// Operations:
    /// - Allocate: O(1) — assigns component, creates handle
    /// - Free: O(1) — removes component, compacts dense array, updates roster
    /// - ResolveHandle: O(1) — validates generation, returns component reference
    ///
    /// Memory: Components array (capacity × sizeof(T)) + Roster array (capacity × 16 bytes)
    ///
    /// Thread Safety: Not thread-safe. All operations must be on the main thread.
    /// </remarks>
    public class ComponentPool<T> : IComponentPool where T : struct
    {
        // ============================================================
        //  PUBLIC STATE
        // ============================================================

        /// <summary>Number of active components (boundary between used and free dense slots).</summary>
        public int Partition = 0;

        /// <summary>Dense array of all component data (active and free slots).</summary>
        public T[] Components;

        /// <summary>Roster mapping handles to dense indices.</summary>
        public RosterItem[] Roster;

        // ============================================================
        //  PRIVATE STATE
        // ============================================================

        /// <summary>Head of the free roster slot list.</summary>
        protected int _freeRosterHead = -1;

        /// <summary>Counter for allocating new roster slots when free list is empty.</summary>
        protected int _rosterIncr = 0;

        /// <summary>Type of the component (for debugging).</summary>
        private readonly System.Type _componentType;

        /// <summary>Type ID of the component pool.</summary>
        private readonly int _poolId;

        /// <summary>Update stages for this component type.</summary>
        private EUpdateStage _updateStages;

        /// <summary>Async update stages for this component type.</summary>
        private EAsyncUpdateStage _asyncUpdateStages;

        /// <summary>Bit mask for group filtering.</summary>
        private uint _mask;

        // ============================================================
        //  CONSTRUCTOR
        // ============================================================

        /// <summary>
        /// Initializes a new component pool with the specified capacity and settings.
        /// </summary>
        /// <param name="capacity">Maximum number of concurrent instances.</param>
        /// <param name="updateStages">Update stages on the main processor.</param>
        /// <param name="asyncUpdateStages">Async update stages.</param>
        /// <param name="mask">Bit mask for group filtering.</param>
        public ComponentPool(
            int capacity,
            EUpdateStage updateStages = EUpdateStage.Update,
            EAsyncUpdateStage asyncUpdateStages = EAsyncUpdateStage.None,
            uint mask = 0)
        {
            _componentType = typeof(T);
            _poolId = ComponentType<T>.Id;
            _updateStages = updateStages;
            _asyncUpdateStages = asyncUpdateStages;
            _mask = mask;

            Components = new T[capacity];
            Roster = new RosterItem[capacity];

            // Initialize free list: all slots are initially free
            for (int i = 0; i < capacity; i++)
                Roster[i].Next = i + 1;
            Roster[capacity - 1].Next = -1;
        }

        // ============================================================
        //  HANDLE RESOLUTION
        // ============================================================

        /// <summary>
        /// Resolves a handle to a component reference.
        /// </summary>
        /// <param name="handle">Handle to resolve.</param>
        /// <returns>Reference to the component.</returns>
        /// <exception cref="System.InvalidCastException">If the handle is stale.</exception>
        /// <remarks>
        /// Validates generation match before returning the component.
        /// If the generation doesn't match, the handle is stale (component was freed).
        ///
        /// Complexity: O(1)
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ResolveHandle(Handle handle)
        {
            int rosterIndex = handle.Id;

            // Validate generation
            if (Roster[rosterIndex].Generation == handle.Generation)
            {
                int denseIndex = Roster[rosterIndex].Index;
                return ref Components[denseIndex];
            }

            throw new System.InvalidCastException(
                $"DCS ValidCast Error: Handle is stale for pool {_componentType.Name}"
            );
        }

        // ============================================================
        //  ALLOCATION
        // ============================================================

        /// <summary>
        /// Allocates a new component.
        /// </summary>
        /// <param name="hostHandle">Host that owns the component.</param>
        /// <param name="chain">Host chain manager.</param>
        /// <returns>Handle to the allocated component.</returns>
        public Handle Allocate(Host hostHandle, HostChain chain)
            => Allocate(hostHandle, chain, null);

        /// <summary>
        /// Allocates a new component with initialization data.
        /// </summary>
        /// <param name="hostHandle">Host that owns the component.</param>
        /// <param name="chain">Host chain manager.</param>
        /// <param name="prius">Initialization data (optional).</param>
        /// <returns>Handle to the allocated component.</returns>
        /// <exception cref="System.Exception">If the pool capacity is exceeded.</exception>
        /// <remarks>
        /// Algorithm:
        /// 1. Takes a roster slot from the free list (or allocates a new one)
        /// 2. Assigns the next dense index (Partition)
        /// 3. Increments generation (protects against stale handles)
        /// 4. Stores the host reference in the roster
        /// 5. Initializes the component (default, sets RosterIndex, calls Init)
        /// 6. Creates a handle and adds the component to the host's chain
        ///
        /// Complexity: O(1)
        /// </remarks>
        public Handle Allocate(Host hostHandle, HostChain chain, object prius)
        {
            if (Partition >= Components.Length)
                throw new System.Exception(
                    $"DCS Error: Pool capacity exceeded for {_componentType.Name}!"
                );

            // Get a roster slot (free list or new)
            int rosterIndex = (_freeRosterHead != -1) ? _freeRosterHead : _rosterIncr++;
            if (_freeRosterHead != -1)
                _freeRosterHead = Roster[_freeRosterHead].Next;

            int denseIndex = Partition++;

            // Setup roster slot
            Roster[rosterIndex].Index = denseIndex;
            Roster[rosterIndex].Generation++;
            Roster[rosterIndex].Host = hostHandle;
            int currentGen = Roster[rosterIndex].Generation;

            // Initialize component
            ref T comp = ref Components[denseIndex];
            comp = default;

            if (comp is IComponent dcsComp)
                dcsComp.RosterIndex = rosterIndex;

            if (prius != null && comp is IInitializable initializable)
                initializable.Init(prius);

            // Add to host chain
            Handle handle = new Handle { Id = rosterIndex, Generation = currentGen };
            chain.Add(hostHandle, handle, _poolId);

            return handle;
        }
        /// <summary>
        /// Allows non-generic allocation via the native Lua bridge
        /// </summary>
        /// <param name="hostHandle"></param>
        /// <param name="chain"></param>
        /// <returns></returns>
        Handle IComponentPool.SystemAllocate(Host hostHandle, HostChain chain)
        {
            return Allocate(hostHandle, chain, null);
        }

        // ============================================================
        //  FREE
        // ============================================================

        /// <summary>
        /// Frees a component and compacts the dense array.
        /// </summary>
        /// <param name="hostHandle">Host that owns the component.</param>
        /// <param name="chain">Host chain manager.</param>
        /// <param name="handle">Handle to the component to free.</param>
        /// <remarks>
        /// Algorithm (Swap-Back):
        /// 1. Removes the component from the host's chain
        /// 2. Increments generation (invalidates all old handles)
        /// 3. Returns the roster slot to the free list
        /// 4. Decrements Partition
        /// 5. If the freed slot wasn't the last, moves the last component
        ///    into the freed slot and updates its RosterIndex
        ///
        /// Complexity: O(1) + O(N) for chain removal (N = components of the host)
        /// </remarks>
        public void Free(Host hostHandle, HostChain chain, ref Handle handle)
        {
            int rosterIndexToDelete = handle.Id;
            int denseIndexToDelete = Roster[rosterIndexToDelete].Index;

            // Remove from host chain
            chain.Remove(Roster[rosterIndexToDelete].Host, handle, _poolId);

            // Invalidate roster slot and return to free list
            Roster[rosterIndexToDelete].Generation++;
            Roster[rosterIndexToDelete].Next = _freeRosterHead;
            Roster[rosterIndexToDelete].Host = default;
            _freeRosterHead = rosterIndexToDelete;

            // Swap-Back: compact the dense array
            Partition--;
            int denseIndexToMove = Partition;

            if (denseIndexToDelete != denseIndexToMove)
            {
                // Move the last component into the deleted slot
                Components[denseIndexToDelete] = Components[denseIndexToMove];

                // Update the roster to point to the new dense index
                if (Components[denseIndexToDelete] is IComponent movingComp)
                {
                    int movingRosterIndex = movingComp.RosterIndex;
                    Roster[movingRosterIndex].Index = denseIndexToDelete;
                }
            }

            handle = default;
        }

        // ============================================================
        //  CLEAR FRAME POOL
        // ============================================================

        /// <summary>
        /// Clears all components from the pool (used for frame-based event pools).
        /// </summary>
        /// <remarks>
        /// Resets the pool to empty state:
        /// 1. Clears the dense array (zeros out active components)
        /// 2. Resets Partition to 0
        /// 3. Resets the roster free list (all slots become free)
        /// 4. Increments generations to invalidate all existing handles
        ///
        /// This is used for event pools that should be reset each frame.
        /// </remarks>
        public virtual void ClearFramePool()
        {
            // Clear dense array
            System.Array.Clear(Components, 0, Partition);

            // Reset state
            Partition = 0;
            _rosterIncr = 0;
            _freeRosterHead = -1;

            // Reset roster: all slots become free with incremented generations
            for (int i = 0; i < Roster.Length; i++)
            {
                Roster[i].Host = default;
                Roster[i].Generation++; // Invalidate all existing handles
                Roster[i].Next = i + 1;
            }
            Roster[Roster.Length - 1].Next = -1;
        }

        // ============================================================
        //  SYSTEMFREE — INTERFACE IMPLEMENTATION
        // ============================================================

        /// <summary>
        /// System-level free called by HostChainManager during host destruction.
        /// </summary>
        void IComponentPool.SystemFree(Host hostHandle, HostChain chain, Handle handle)
        {
            // Validate handle before freeing
            if (Roster[handle.Id].Generation != handle.Generation)
                return; // Handle is stale, component already freed

            Handle handleCopy = handle;
            Free(hostHandle, chain, ref handleCopy);
        }

        // ============================================================
        //  SYSTEMDELIVER — INTERFACE IMPLEMENTATION
        // ============================================================

        /// <summary>
        /// Delivers a message to a component at the specified roster index.
        /// </summary>
        /// <param name="rosterIndex">Roster index of the receiver.</param>
        /// <param name="msgTypeId">Type ID of the message.</param>
        /// <param name="msgHandle">Message handle with generation validation.</param>
        /// <remarks>
        /// Called by EventSystem during message delivery.
        /// Validates the receiver's generation before dispatching.
        /// If the receiver implements IDcsMessageReceiver, delivers the message.
        ///
        /// Complexity: O(1)
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DeliverDirect(int rosterIndex, int msgTypeId, Handle msgHandle)
        {
            // Validate receiver generation
            if (Roster[rosterIndex].Generation != msgHandle.Generation)
                return;

            int denseIndex = Roster[rosterIndex].Index;

            // Deliver message if receiver implements the interface
            if (Components[denseIndex] is IMessageReceiver receiver)
            {
                receiver.ReceiveMessage(msgTypeId, msgHandle);
            }
        }

        /// <summary>
        /// System-level message delivery (wraps DeliverDirect).
        /// </summary>
        public void SystemDeliver(int rosterIndex, int msgTypeId, Handle msgHandle)
            => DeliverDirect(rosterIndex, msgTypeId, msgHandle);
    }
}