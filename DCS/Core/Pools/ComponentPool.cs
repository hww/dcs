using System.Runtime.CompilerServices;
using System;
using UnityEngine;
using DCS.Lua;

namespace DCS.Core
{
    // ============================================================
    //  ROSTER ITEM — ROSTER SLOT
    // ============================================================

    /// <summary>
    /// A slot in the Roster, linking handles to dense component data.
    /// </summary>
    /// <remarks>
    /// Each RosterItem represents a stable slot that maps:
    /// - DcsHandle (Id + Generation) → Component data (Index)
    /// - Free-list link (Next) when the slot is free
    /// - Used-list links (Next, Prev) when the slot is used
    ///
    /// The Roster is the "sparse array" that provides stable handles
    /// while the component data (Dense Array) can be compacted via Swap-Back.
    ///
    /// Memory: 8 bytes (4 × ushort)
    ///
    /// Note: Host is intentionally NOT stored here — it lives inside the
    /// component itself (via the optional IComponent.Host field), so that
    /// IComponent is no longer a required constraint.
    /// </remarks>
    public struct RosterItem
    {
        /// <summary>Index into the dense component array (DenseIndex).</summary>
        public ushort Index;

        /// <summary>Generation for handle validation. Incremented on each free.</summary>
        public ushort Generation;

        /// <summary>
        /// Next slot in the containing list (free-list or used-list, depending on state).
        /// </summary>
        public ushort Next;

        /// <summary>
        /// Previous slot in the used-list. Unused when the slot is free.
        /// </summary>
        public ushort Prev;
    }

    // ============================================================
    //  ROSTER LINK — HEAD/TAIL PAIR FOR INTRUSIVE LISTS
    // ============================================================

    /// <summary>
    /// Head/Tail pair for an intrusive doubly-linked list stored in RosterItem.
    /// </summary>
    public struct RosterLink
    {
        public ushort Head;
        public ushort Tail;

        public const ushort Null = 0xFFFF;

        public static RosterLink Empty => new RosterLink { Head = Null, Tail = Null };
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Head == Null;
        } 
    }

    // ============================================================
    //  COMPONENT MANAGER — DENSE POOL WITH SPARSE ROSTER
    // ============================================================

    /// <summary>
    /// High-performance component pool with dense array storage and sparse roster handles.
    /// </summary>
    /// <typeparam name="T">Component type (struct). IComponent is no longer required.</typeparam>
    /// <remarks>
    /// Architecture:
    /// - Dense Array: Contiguous storage of active components (cache-friendly iteration)
    /// - Sparse Roster: Stable handles with generation (safe references)
    /// - Used-list: Intrusive doubly-linked list over roster slots (order == dense order)
    /// - Free-list: Intrusive singly-linked list over roster slots (reuses slots)
    /// - Swap-Back: O(1) removal with compaction, driven entirely by the used-list
    ///
    /// The used-list invariant is: iterating Head → Tail yields roster slots whose
    /// RosterItem.Index equals 0, 1, 2, ... Partition-1 in order. Therefore:
    ///   - usedList.Tail  == roster slot of the last dense component
    ///   - Partition      == used-list length
    ///
    /// This lets Free() find the last component's roster slot in O(1) without
    /// reading RosterIndex from the component, so IComponent is no longer needed.
    ///
    /// Thread Safety: Not thread-safe. All operations must be on the main thread.
    /// </remarks>
    public class ComponentPool<T> : IComponentPool, IFieldAccessForIndex where T : struct
    {
        // ============================================================
        //  CONSTANTS
        // ============================================================

        private const ushort NULL = RosterLink.Null;

        // ============================================================
        //  PUBLIC STATE
        // ============================================================

        /// <summary>Number of active components (== length of used-list).</summary>
        public ushort Partition = 0;

        /// <summary>Dense array of all component data (active and free slots).</summary>
        public T[] Components;

        /// <summary>Roster mapping handles to dense indices.</summary>
        public RosterItem[] Roster;

        // ============================================================
        //  PRIVATE STATE
        // ============================================================

        /// <summary>Head of the free roster slot list (singly-linked via RosterItem.Next).</summary>
        protected ushort _freeRosterHead = NULL;

        /// <summary>Counter for allocating new roster slots when free list is empty.</summary>
        protected int _rosterIncr = 0;

        /// <summary>Intrusive doubly-linked list of used roster slots (order == dense order).</summary>
        protected RosterLink _usedList = RosterLink.Empty;

        /// <summary>Type of the component (for debugging).</summary>
        private readonly System.Type _componentType;

        /// <summary>Type ID of the component pool.</summary>
        private int _poolId;

        /// <summary>Update stages for this component type.</summary>
        private EUpdateStage _updateStages;

        /// <summary>Async update stages for this component type.</summary>
        private EAsyncUpdateStage _asyncUpdateStages;

        /// <summary>Bit mask for group filtering.</summary>
        private uint _mask;

        // Thread-safe high-performance compiled delegates for safe unboxed marshalling
        private static RefFieldGetter<T> _compiledGetField;
        private static RefFieldSetter<T> _compiledSetField;

        // Compiled message-receiver bridge (null if T does not implement IMessageReceiver).
        private static MessageReceiverBridge<T> _receiverBridge;

        private delegate void MessageReceiverBridge<TComp>(ref TComp comp, int msgTypeId, Handle msgHandle);

        // ============================================================
        //  INITIALIZER
        // ============================================================

        public delegate void InitDelegate(ref T comp, object prius);
        public InitDelegate InitCallback;

        // ============================================================
        //  STATIC CONSTRUCTOR
        // ============================================================

        static ComponentPool()
        {
            BuildAccessors();
            BuildReceiverBridge();
        }

        // ============================================================
        //  CONSTRUCTOR
        // ============================================================

        public ComponentPool(
            int capacity,
            EUpdateStage updateStages = EUpdateStage.Update,
            EAsyncUpdateStage asyncUpdateStages = EAsyncUpdateStage.None,
            uint mask = 0)
        {
            _componentType = typeof(T);
            _updateStages = updateStages;
            _asyncUpdateStages = asyncUpdateStages;
            _mask = mask;

            Components = new T[capacity];
            Roster = new RosterItem[capacity];

            // Initialize free list: all slots are initially free
            for (int i = 0; i < capacity; i++)
            {
                Roster[i].Next = (ushort)(i + 1);
                Roster[i].Prev = NULL;
                Roster[i].Index = 0;
                Roster[i].Generation = 0;
            }
            Roster[capacity - 1].Next = NULL;

            _freeRosterHead = 0;
            _usedList = RosterLink.Empty;
        }

        // ============================================================
        //  USED-LIST HELPERS (INTRUSIVE DOUBLY-LINKED LIST)
        // ============================================================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UsedListAppend(ushort slot)
        {
            ref RosterItem item = ref Roster[slot];
            item.Next = NULL;
            item.Prev = _usedList.Tail;

            if (_usedList.Tail == NULL)
            {
                _usedList.Head = slot;
            }
            else
            {
                Roster[_usedList.Tail].Next = slot;
            }
            _usedList.Tail = slot;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UsedListRemove(ushort slot)
        {
            ref RosterItem item = ref Roster[slot];

            if (item.Prev == NULL)
                _usedList.Head = item.Next;
            else
                Roster[item.Prev].Next = item.Next;

            if (item.Next == NULL)
                _usedList.Tail = item.Prev;
            else
                Roster[item.Next].Prev = item.Prev;

            item.Prev = NULL;
            item.Next = NULL;
        }

        // ============================================================
        //  FREE-LIST HELPERS (INTRUSIVE SINGLY-LINKED LIST)
        // ============================================================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort FreeListPop()
        {
            if (_freeRosterHead != NULL)
            {
                ushort slot = _freeRosterHead;
                _freeRosterHead = Roster[slot].Next;
                Roster[slot].Next = NULL;
                Roster[slot].Prev = NULL;
                return slot;
            }

            // No free slots — allocate a new one.
            if (_rosterIncr >= Roster.Length)
                throw new System.Exception(
                    $"DCS Error: Pool capacity exceeded for {_componentType.Name}!"
                );

            ushort fresh = (ushort)_rosterIncr++;
            Roster[fresh].Next = NULL;
            Roster[fresh].Prev = NULL;
            return fresh;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FreeListPush(ushort slot)
        {
            Roster[slot].Prev = NULL;
            Roster[slot].Next = _freeRosterHead;
            _freeRosterHead = slot;
        }

        // ============================================================
        //  HANDLE RESOLUTION
        // ============================================================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ResolveHandle(Handle handle)
        {
            int rosterIndex = handle.Id;

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

        public Handle Allocate(Host hostHandle, HostChain chain)
            => Allocate(hostHandle, chain, null);

        /// <summary>
        /// Allocates a new component.
        /// </summary>
        /// <remarks>
        /// Algorithm:
        /// 1. Pop a roster slot from the free list (or bump _rosterIncr).
        /// 2. Assign the next dense index (Partition).
        /// 3. Increment generation (protects against stale handles).
        /// 4. Append the roster slot to the used-list.
        /// 5. Initialize the component (default, Init callback).
        /// 6. Create a handle and add the component to the host chain.
        ///
        /// Note: Host is no longer stored in RosterItem. Callers that need the
        /// owner should keep it inside the component itself.
        /// </remarks>
        public Handle Allocate(Host hostHandle, HostChain chain, object prius)
        {
            if (Partition >= Components.Length)
                throw new System.Exception(
                    $"DCS Error: Pool capacity exceeded for {_componentType.Name}!"
                );

            ushort rosterIndex = FreeListPop();
            int denseIndex = Partition++;

            ref RosterItem slot = ref Roster[rosterIndex];
            slot.Index = (ushort)denseIndex;
            slot.Generation++;

            UsedListAppend(rosterIndex);

            // Initialize component
            ref T comp = ref Components[denseIndex];
            comp = default;

            if (InitCallback != null)
                InitCallback(ref comp, prius);

            // Add to host chain
            Handle handle = new Handle
            {
                Id = rosterIndex,
                Generation = slot.Generation
            };
            chain.Add(hostHandle, handle, _poolId);

            return handle;
        }

        public Handle SystemAllocate(Host hostHandle, HostChain chain)
            => Allocate(hostHandle, chain, null);

        public Handle SystemAllocate(Host hostHandle, HostChain chain, object prius)
            => Allocate(hostHandle, chain, prius);

        // ============================================================
        //  FREE
        // ============================================================

        /// <summary>
        /// Frees a component and compacts the dense array via swap-back.
        /// </summary>
        /// <remarks>
        /// Algorithm (Swap-Back, entirely roster-driven):
        /// 1. Remove the component from the host's chain.
        /// 2. Increment generation (invalidates all old handles).
        /// 3. Determine lastRoster = _usedList.Tail, lastDense = Roster[lastRoster].Index.
        /// 4. If deleted != last, move Components[lastDense] → Components[deletedDense]
        ///    and update Roster[lastRoster].Index = deletedDense.
        /// 5. Remove the deleted slot from the used-list and push it to the free-list.
        /// 6. Partition--.
        ///
        /// Note: no component field (RosterIndex) is ever read here, which is
        /// what makes IComponent optional.
        ///
        /// Complexity: O(1) + O(N) for chain removal (N = components of the host).
        /// </remarks>
        public void Free(Host hostHandle, HostChain chain, ref Handle handle)
        {
            ushort deletedRoster = handle.Id;
            int deletedDense = Roster[deletedRoster].Index;

            // Remove from host chain (uses handle before invalidating it).
            chain.Remove(hostHandle, handle, _poolId);

            // Invalidate roster slot.
            Roster[deletedRoster].Generation++;
            Roster[deletedRoster].Index = 0;

            // Determine the last used slot (== last dense component).
            ushort lastRoster = _usedList.Tail;
            Partition--;

            if (lastRoster != deletedRoster)
            {
                int lastDense = Roster[lastRoster].Index;

                // Move the last component into the deleted dense slot.
                Components[deletedDense] = Components[lastDense];

                // The moving slot now points to the deleted dense index.
                Roster[lastRoster].Index = (ushort)deletedDense;
                // (Its position in the used-list does not change — the slot is
                //  still the tail; only the dense index it points to changed.)

                // Remove the deleted slot from the used-list (it is NOT the tail).
                UsedListRemove(deletedRoster);
            }
            else
            {
                // Deleted slot is the tail — just detach it.
                UsedListRemove(deletedRoster);
            }

            // Return the deleted slot to the free-list.
            FreeListPush(deletedRoster);

            handle = default;
        }

        // ============================================================
        //  CLEAR FRAME POOL
        // ============================================================

        public virtual void ClearFramePool()
        {
            System.Array.Clear(Components, 0, Partition);

            Partition = 0;
            _rosterIncr = 0;
            _freeRosterHead = 0;
            _usedList = RosterLink.Empty;

            for (int i = 0; i < Roster.Length; i++)
            {
                ref RosterItem item = ref Roster[i];
                item.Index = 0;
                item.Generation++;   // invalidate all existing handles
                item.Next = (ushort)(i + 1);
                item.Prev = NULL;
            }
            Roster[Roster.Length - 1].Next = NULL;
        }

        // ============================================================
        //  SYSTEMFREE — INTERFACE IMPLEMENTATION
        // ============================================================

        void IComponentPool.SystemFree(Host hostHandle, HostChain chain, Handle handle)
        {
            if (Roster[handle.Id].Generation != handle.Generation)
                return;

            Handle handleCopy = handle;
            Free(hostHandle, chain, ref handleCopy);
        }

        // ============================================================
        //  SYSTEMDELIVER — INTERFACE IMPLEMENTATION
        // ============================================================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DeliverDirect(int rosterIndex, int msgTypeId, Handle msgHandle)
        {
            if (Roster[rosterIndex].Generation != msgHandle.Generation)
                return;

            if (_receiverBridge == null)
                return;

            int denseIndex = Roster[rosterIndex].Index;
            _receiverBridge(ref Components[denseIndex], msgTypeId, msgHandle);
        }

        public void SystemDeliver(int rosterIndex, int msgTypeId, Handle msgHandle)
            => DeliverDirect(rosterIndex, msgTypeId, msgHandle);

        // ============================================================
        //  HANDLE → DENSE INDEX
        // ============================================================

        public bool TryGetDenseIndex(Handle handle, out int denseIndex)
        {
            denseIndex = -1;

            if (handle.Id < 0 || handle.Id >= Roster.Length)
                return false;

            ref RosterItem slot = ref Roster[handle.Id];

            if (slot.Generation != handle.Generation)
                return false;

            if (slot.Index == HandleConfig.NULL_INDEX)
                return false;

            denseIndex = slot.Index;
            return true;
        }

        // ============================================================
        //  FIELD ACCESS
        // ============================================================

        public virtual bool GetField(int denseIndex, string fieldName, IntPtr L)
        {
            if (_compiledGetField == null) return false;
            return _compiledGetField(ref Components[denseIndex], fieldName, L);
        }

        public virtual bool SetField(int denseIndex, string fieldName, IntPtr L)
        {
            if (_compiledSetField == null) return false;
            return _compiledSetField(ref Components[denseIndex], fieldName, L);
        }

        // ============================================================
        //  STATIC ACCESSOR PIPELINES
        // ============================================================

        private static void BuildAccessors()
        {
            try
            {
                _compiledGetField = FieldExpressionFactory.CreateGetter<T>();
                _compiledSetField = FieldExpressionFactory.CreateSetter<T>();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(
                    $"[DCS Registry Error] Failed to generate fast field accessors " +
                    $"for type {typeof(T).Name}: {ex.Message}");
            }
        }

        private static void BuildReceiverBridge()
        {
            // If T implements IMessageReceiver, compile a direct bridge once.
            if (!typeof(IMessageReceiver).IsAssignableFrom(typeof(T)))
            {
                _receiverBridge = null;
                return;
            }

            _receiverBridge = (ref T comp, int msgTypeId, Handle msgHandle) =>
            {
                // Boxing-free cast via constrained call is not expressible in a
                // generic delegate directly; use Unsafe.As to reinterpret the
                // struct as its interface reference without allocation.
                ref IMessageReceiver asReceiver = ref Unsafe.As<T, IMessageReceiver>(ref comp);
                asReceiver.ReceiveMessage(msgTypeId, msgHandle);
            };
        }

        // ============================================================
        //  DIAGNOSTICS
        // ============================================================

        /// <summary>
        /// Iterates the used-list in dense order. Debug/validation use only.
        /// </summary>
        public void ForEachUsedRosterSlot(Action<ushort, int> visit)
        {
            ushort cur = _usedList.Head;
            int expectedDense = 0;
            while (cur != NULL)
            {
                visit(cur, expectedDense);
                if (Roster[cur].Index != expectedDense)
                {
                    Debug.LogError(
                        $"[DCS Roster Invariant] slot {cur} has dense {Roster[cur].Index} " +
                        $"but expected {expectedDense} (pool {_componentType.Name})");
                }
                expectedDense++;
                cur = Roster[cur].Next;
            }
        }

        public void SetPoolId(int newId)
        {
            _poolId = newId;
        }
    }
}