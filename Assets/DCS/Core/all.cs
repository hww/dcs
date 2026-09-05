using UnityEngine;

namespace DynamicComponent
{
	/// <summary>
	/// Base attribute for configuring component pool memory layout.
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
	public class BasePoolAttribute : System.Attribute
	{
		/// <summary>Initial pool capacity (maximum concurrent instances).</summary>
		public int Capacity { get; }

		/// <summary>Bit mask for group filtering (pause, categories).</summary>
		public uint Mask { get; }

		/// <summary>Update stages on the main processor (PPU).</summary>
		public EUpdateStage UpdateStage { get; }

		/// <summary>Asynchronous update stages (SPU / Job System).</summary>
		public EAsyncUpdateStage AsyncUpdateStage { get; }

		public BasePoolAttribute(
			int capacity = 1000,
			EUpdateStage updateStage = EUpdateStage.Update,
			EAsyncUpdateStage asyncUpdateStage = EAsyncUpdateStage.None,
			uint mask = 0)
		{
			Capacity = capacity;
			UpdateStage = updateStage;
			AsyncUpdateStage = asyncUpdateStage;
			Mask = mask;
		}
	}

	/// <summary>
	/// Attribute for persistent components (data, physics, transforms).
	/// </summary>
	public class ComponentPoolAttribute : BasePoolAttribute
	{
		public ComponentPoolAttribute(
			int capacity = 1000,
			EUpdateStage updateStage = EUpdateStage.Update,
			EAsyncUpdateStage asyncUpdateStage = EAsyncUpdateStage.None,
			uint mask = 0)
			: base(capacity, updateStage, asyncUpdateStage, mask) { }
	}

	/// <summary>
	/// Attribute for event components (messages, signals).
	/// </summary>
	public class MessagePoolAttribute : BasePoolAttribute
	{
		public MessagePoolAttribute(
			int capacity = 1000,
			EUpdateStage updateStage = EUpdateStage.Update,
			EAsyncUpdateStage asyncUpdateStage = EAsyncUpdateStage.None,
			uint mask = 0)
			: base(capacity, updateStage, asyncUpdateStage, mask) { }
	}
}namespace DynamicComponent
{
    /// <summary>
    /// Update phases on the main processor (PPU).
    /// </summary>
    public enum EUpdateStage
    {
        /// <summary>No update.</summary>
        None,

        /// <summary>Main frame update (Time.deltaTime).</summary>
        Update,

        /// <summary>Fixed timestep update (Time.fixedDeltaTime).</summary>
        FixedUpdate,

        /// <summary>Post-render update.</summary>
        PostUpdate
    }

    /// <summary>
    /// Asynchronous update phases (SPU / Job System).
    /// </summary>
    public enum EAsyncUpdateStage
    {
        /// <summary>No async update.</summary>
        None,

        /// <summary>Async main frame update.</summary>
        Update,

        /// <summary>Async fixed timestep update.</summary>
        FixedUpdate,

        /// <summary>Async post-render update.</summary>
        PostUpdate
    }
}namespace DynamicComponent
{
    /// <summary>
    /// Safe reference to a game object (Host).
    /// </summary>
    /// <remarks>
    /// Host is the owner of components (e.g., player, enemy, bullet).
    /// Stores only an identifier and generation to protect against stale references.
    /// </remarks>
    public struct Host
    {
        public int Id;
        public int Generation;
        public bool IsNull => Generation == 0;
    }

    /// <summary>
    /// Safe reference to a component in a pool.
    /// </summary>
    /// <remarks>
    /// The handle contains an index into the Roster and a Generation.
    /// Generation is incremented each time a slot is freed, making
    /// old handles invalid (IsNull == true).
    /// This protects against accessing reused components.
    /// </remarks>
    public struct Handle
    {
        /// <summary>Index into the Roster array.</summary>
        public int Id;

        /// <summary>Roster slot generation. Incremented on each free.</summary>
        public int Generation;

        /// <summary>Checks whether the handle is valid.</summary>
        /// <returns>True if Generation is 0 (uninitialized or stale).</returns>
        public bool IsNull => Generation == 0;
    }

    /// <summary>
    /// Handle for messages (events) passed between components.
    /// </summary>
    /// <remarks>
    /// Unlike DcsHandle, contains TypeId for dispatching.
    /// Used in EventSystem to deliver messages to receivers.
    /// </remarks>
    public struct TypedHandle
    {
        /// <summary>
        /// Event type (ComponentType{T}.Id).
        /// Used for switch-based dispatching.
        /// </summary>
        public int TypeId;

        /// <summary>Roster index of the source component.</summary>
        public int Id;

        /// <summary>Generation of the source component.</summary>
        public int Generation;

        /// <summary>Checks whether the message handle is valid.</summary>
        public bool IsNull => Generation == 0;
    }
}namespace DynamicComponent
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
}namespace DynamicComponent
{
    // ============================================================
    //  COMPONENT INTERFACES
    // ============================================================

    /// <summary>
    /// Base interface for all DCS components.
    /// Required for storage in ComponentManager and EventManager pools.
    /// </summary>
    /// <remarks>
    /// Every component must store its index in the Roster (RosterIndex).
    /// This is required for correct reference updates during Swap-Back
    /// when a component is removed from the dense pool.
    /// </remarks>
    public interface IComponent
    {
        /// <summary>
        /// Component index in the Roster array.
        /// Set during allocation and updated when moved within the pool.
        /// </summary>
        int RosterIndex { get; set; }
    }

    /// <summary>
    /// Interface for components that support initialization via Prius.
    /// </summary>
    /// <remarks>
    /// Prius is an arbitrary object passed to Allocate().
    /// Used to pass initialization data (e.g., NavQuery, configs).
    /// </remarks>
    public interface IInitializable
    {
        /// <summary>
        /// Initializes the component with the provided data.
        /// </summary>
        /// <param name="prius">Initialization data object (can be null).</param>
        void Init(object prius);
    }

    // ============================================================
    //  EVENT INTERFACES
    // ============================================================

    /// <summary>
    /// Marker interface for event components.
    /// </summary>
    /// <remarks>
    /// Events are short-lived components (typically 1 frame).
    /// They are stored in separate pools (EventManager) and processed via EventSystem.
    /// Key feature: NamespaceMask for subscription filtering.
    /// </remarks>
    public interface IEvent : IComponent
    {
        /// <summary>
        /// Namespace mask for subscription filtering.
        /// </summary>
        /// <remarks>
        /// Used in EventSystem.PollEvents for checking:
        /// (ev.NamespaceMask & sub.NamespaceMask) != 0
        /// </remarks>
        uint NamespaceMask { get; set; }
    }

    // ============================================================
    //  MESSAGE RECEIVER
    // ============================================================

    /// <summary>
    /// Interface for components that can receive messages (events).
    /// </summary>
    /// <remarks>
    /// Implemented by processes (FSM) and states subscribed to events.
    /// ReceiveMessage is called by the event delivery system (EventSystem).
    /// </remarks>
    public interface IMessageReceiver
    {
        /// <summary>
        /// Handles an incoming message.
        /// </summary>
        /// <param name="msgHandle">Message handle containing type and data.</param>
        void ReceiveMessage(int msgTypeId, Handle msgHandle);
    }

    // ============================================================
    //  EVENT DISPATCHER
    // ============================================================

    /// <summary>
    /// Interface for dispatching events from event pools.
    /// </summary>
    /// <remarks>
    /// Implemented by event managers (EventManager{T}) for integration with the delivery system.
    /// Called from EventSystem.UpdateComponents during the Update phase.
    ///
    /// Lifecycle:
    /// 1. SystemPoll() collects all active events from the pool
    /// 2. Matches them with subscriptions via TypeChainManager
    /// 3. Builds an invocation list (InvokeList)
    /// 4. EventSystem.DeliverEvents() delivers them to receivers
    /// </remarks>
    public interface IEventDispatcher
    {
        /// <summary>
        /// Polls the event pool to collect invocations.
        /// </summary>
        /// <param name="subManager">Subscription manager (contains SubscriptionNode).</param>
        /// <param name="typeChain">Type chain manager (links events to subscriptions).</param>
        /// <remarks>
        /// Called by the system during the Update phase.
        /// The method should:
        /// - Iterate all active events in the pool
        /// - Find subscriptions via typeChain.GetTypeChainHead(eventTypeId)
        /// - Check mask matches (ev.NamespaceMask & sub.NamespaceMask)
        /// - Add records to EventSystem._invokeList for later delivery
        /// </remarks>
        void SystemPoll(EventSubscription subManager, TypeChain typeChain);
    }
}using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;
using DynamicComponent;

namespace DynamicComponent
{
    // ============================================================
    //  ICOMPONENTPOOL — POOL INTERFACE
    // ============================================================

    /// <summary>
    /// Interface for all component pools, used for lifecycle management.
    /// </summary>
    /// <remarks>
    /// Provides a non-generic interface for operations that work with any pool:
    /// - SystemFree: Called by HostChainManager when a host is destroyed
    /// - ClearFramePool: Resets frame-based event pools
    /// - SystemDeliver: Delivers messages to receivers
    ///
    /// This interface enables type-safe access to pools without knowing T.
    /// </remarks>
    public interface IComponentPool
    {
        /// <summary>
        /// Frees a component as part of host destruction.
        /// </summary>
        /// <param name="hostHandle">Host that owns the component.</param>
        /// <param name="chain">Host chain manager.</param>
        /// <param name="handle">Handle to the component to free.</param>
        void SystemFree(Host hostHandle, HostChain chain, Handle handle);

        /// <summary>
        /// Clears all frame-based components from the pool.
        /// </summary>
        void ClearFramePool();

        /// <summary>
        /// Delivers a message to a component at the specified roster index.
        /// </summary>
        /// <param name="rosterIndex">Index in the Roster.</param>
        /// <param name="msgTypeId">Type ID of the message.</param>
        /// <param name="msgHandle">Message handle with generation validation.</param>
        void SystemDeliver(int rosterIndex, int msgTypeId, Handle msgHandle);
    }

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
}using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

namespace DynamicComponent
{
    // ============================================================
    //  COMPONENT TYPE — TYPE ID GENERATOR
    // ============================================================

    /// <summary>
    /// Auto-generates and registers a unique type ID for each component type.
    /// </summary>
    /// <typeparam name="T">Component type to register.</typeparam>
    /// <remarks>
    /// The static constructor is triggered by RuntimeHelpers.RunClassConstructor
    /// during pool initialization, registering the type and creating its pool.
    ///
    /// This design ensures:
    /// - Type IDs are assigned at compile-time (via static constructor)
    /// - No runtime reflection in hot paths
    /// - Type-safe access to pools
    ///
    /// Usage: ComponentType{MyComponent}.Id returns the unique type ID.
    /// </remarks>
    public static class ComponentType<T> where T : struct
    {
        /// <summary>Unique type identifier for T.</summary>
        public static readonly int Id = ComponentRegistry.RegisterNewType<T>();
    }

    // ============================================================
    //  COMPONENT REGISTRY — CENTRAL TYPE AND POOL REGISTRY
    // ============================================================

    /// <summary>
    /// Central registry for all component types and their pools.
    /// </summary>
    /// <remarks>
    /// Manages:
    /// - Unique type IDs (assigned during static initialization)
    /// - Pool instances for each component type
    /// - Event type tracking for polling
    ///
    /// Initialization flow:
    /// 1. ComponentType{T}.Id triggers RegisterNewType{T}
    /// 2. RegisterNewType creates the appropriate pool
    /// 3. InitializeAllPools scans assemblies for DcsPoolAttribute
    /// 4. Forces static constructor execution for all registered types
    ///
    /// This design provides O(1) pool access by TypeId.
    /// </remarks>
    public static class ComponentRegistry
    {
        /// <summary>Internal type counter for assigning unique IDs.</summary>
        private static int _typeCounter = 0;

        /// <summary>Maximum number of component types.</summary>
        public const int MaxComponentTypes = 200;

        /// <summary>Array of all component pools, indexed by TypeId.</summary>
        public static readonly IComponentPool[] Pools = new IComponentPool[MaxComponentTypes];

        /// <summary>Array of event type IDs for polling.</summary>
        public static readonly int[] PollTypeIds = new int[MaxComponentTypes];

        /// <summary>Number of registered event types.</summary>
        public static int PollTypesCount = 0;

        /// <summary>
        /// Initializes all pools by scanning assemblies for DcsPoolAttribute.
        /// </summary>
        /// <remarks>
        /// This must be called once at startup (e.g., in the game initialization).
        /// It:
        /// 1. Scans all types in the executing assembly
        /// 2. Finds structs with DcsPoolAttribute
        /// 3. Triggers their static constructor via RuntimeHelpers.RunClassConstructor
        /// 4. Collects event types into PollTypeIds for fast iteration
        ///
        /// After this call, all pools are ready for use.
        /// </remarks>
        public static void InitializeAllPools()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();

            foreach (var type in types)
            {
                var poolAttribute = type.GetCustomAttribute<BasePoolAttribute>();

                if (type.IsValueType && poolAttribute != null)
                {
                    // Force static constructor execution to register the type
                    var genericComponentType = typeof(ComponentType<>).MakeGenericType(type);
                    RuntimeHelpers.RunClassConstructor(genericComponentType.TypeHandle);

                    // If it's an event type, add it to the polling list
                    if (typeof(IEvent).IsAssignableFrom(type))
                    {
                        var idField = genericComponentType.GetField(
                            "Id",
                            BindingFlags.Public | BindingFlags.Static
                        );
                        PollTypeIds[PollTypesCount++] = (int)idField.GetValue(null);
                    }
                }
            }

            Debug.Log($"<color=green>[DCS SUCCESS]</color> Pools allocated. Total types: {_typeCounter}");
        }

        /// <summary>
        /// Registers a new component type and creates its pool.
        /// </summary>
        /// <typeparam name="T">Component type to register.</typeparam>
        /// <returns>Unique TypeId for T.</returns>
        /// <exception cref="System.Exception">If the type limit is exceeded.</exception>
        /// <remarks>
        /// Called automatically by ComponentType{T}.Id static constructor.
        ///
        /// Algorithm:
        /// 1. Assigns a new TypeId
        /// 2. Reads DcsPoolAttribute for capacity and settings
        /// 3. Creates either:
        ///    - EventManager{T} if T implements IEventData
        ///    - ComponentManager{T} for regular components
        /// 4. Stores the pool in the Pools array at the TypeId index
        ///
        /// Complexity: O(1)
        /// </remarks>
        public static int RegisterNewType<T>() where T : struct
        {
            int newId = _typeCounter++;
            if (newId >= MaxComponentTypes)
                throw new System.Exception("DCS Error: Component type limit exceeded!");

            // Get capacity from attribute or use default
            int capacity = 1000;
            var attr = typeof(T).GetCustomAttribute<BasePoolAttribute>();
            if (attr != null)
                capacity = attr.Capacity;

            // Create the appropriate pool type
            if (typeof(IEvent).IsAssignableFrom(typeof(T)))
            {
                var eventManagerType = typeof(EventPool<>).MakeGenericType(typeof(T));
                Pools[newId] = (IComponentPool)System.Activator.CreateInstance(eventManagerType, capacity);
            }
            else
            {
                Pools[newId] = new ComponentPool<T>(capacity);
            }

            return newId;
        }

        /// <summary>
        /// Gets the typed pool for component type T.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <returns>Typed component manager for T.</returns>
        /// <remarks>
        /// This is the primary way to access pools in hot paths.
        /// Uses aggressive inlining for zero-overhead access.
        ///
        /// Complexity: O(1)
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentPool<T> GetPool<T>() where T : struct
        {
            return (ComponentPool<T>)Pools[ComponentType<T>.Id];
        }
    }
}using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

namespace DynamicComponent
{
    // ============================================================
    //  DYNAMIC COMPONENT SYSTEM — PUBLIC API
    // ============================================================

    /// <summary>
    /// Main public API for the Dynamic Component System.
    /// </summary>
    /// <remarks>
    /// Provides type-safe, high-performance operations for:
    /// - Allocating components
    /// - Resolving handles to component references
    /// - Freeing components and chains
    /// - Updating and dispatching events
    ///
    /// All operations are generic and inlined for zero-overhead access.
    /// This is the primary interface for game code to interact with DCS.
    /// </remarks>
    public static class DCS
    {
        // ============================================================
        //  COMPONENT OPERATIONS
        // ============================================================

        /// <summary>
        /// Gets the first component of type T from a host's chain.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="hostHandle">Host to query.</param>
        /// <param name="chain">Host chain manager.</param>
        /// <returns>Handle to the component, or default if not found.</returns>
        /// <remarks>
        /// Scans the host's component chain for the first component of type T.
        /// This is O(N) where N is the number of components of the host.
        /// Use this for accessing singleton components on a host.
        /// </remarks>
        public static Handle Get<T>(Host hostHandle, HostChain chain) where T : struct
        {
            ChainNode typed = chain.GetTypedHandle(hostHandle, ComponentType<T>.Id);
            return typed.Component;
        }

        /// <summary>
        /// Allocates a new component of type T and adds it to the host's chain.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="hostHandle">Host that owns the component.</param>
        /// <param name="chain">Host chain manager.</param>
        /// <returns>Handle to the allocated component.</returns>
        public static Handle Allocate<T>(Host hostHandle, HostChain chain) where T : struct
        {
            return ComponentRegistry.GetPool<T>().Allocate(hostHandle, chain);
        }

        /// <summary>
        /// Resolves a handle to a component reference.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="handle">Handle to resolve.</param>
        /// <returns>Reference to the component.</returns>
        /// <exception cref="System.InvalidCastException">If the handle is stale.</exception>
        /// <remarks>
        /// This is the primary way to access component data in hot paths.
        /// The method is aggressively inlined for zero-overhead access.
        ///
        /// Performance: O(1) — single array lookup + generation check.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ResolveHandle<T>(Handle handle) where T : struct
        {
            return ref ComponentRegistry.GetPool<T>().ResolveHandle(handle);
        }

        /// <summary>
        /// Frees a component and removes it from the host's chain.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="hostHandle">Host that owns the component.</param>
        /// <param name="chain">Host chain manager.</param>
        /// <param name="handle">Handle to the component to free.</param>
        public static void Free<T>(Host hostHandle, HostChain chain, ref Handle handle) where T : struct
        {
            ComponentRegistry.GetPool<T>().Free(hostHandle, chain, ref handle);
        }

        /// <summary>
        /// Frees all components of a host.
        /// </summary>
        /// <param name="hostHandle">Host to destroy.</param>
        /// <param name="chain">Host chain manager.</param>
        /// <remarks>
        /// Called automatically by HostManager.DestroyHost.
        /// Iterates all components in the host's chain and frees them.
        /// </remarks>
        public static void FreeChain(Host hostHandle, HostChain chain)
        {
            chain.FreeChain(hostHandle);
        }

        // ============================================================
        //  EVENT SYSTEM UPDATES
        // ============================================================
        /// <summary>
        /// Updates the event system for the specified stage using sorted type order.
        /// </summary>
        /// <param name="stage">Update stage to process.</param>
        /// <param name="scheduler">Update scheduler providing sorted type order.</param>
        /// <param name="subManager">Subscription manager.</param>
        /// <param name="typeChain">Type chain manager.</param>
        /// <param name="chain">Host chain manager.</param>
        /// <param name="mask">Context mask for filtering.</param>
        /// <remarks>
        /// Two-phase event processing with priority ordering:
        ///
        /// Update phase:
        /// 1. Iterates component types in priority order (lowest Order first)
        /// 2. Polls each event pool for matching subscriptions (Phase A)
        /// 3. Collects matching subscriptions via TypeChainManager
        /// 4. Builds the invocation list
        /// 5. Delivers all events (Phase B)
        ///
        /// PostUpdate phase:
        /// 1. Clears all frame-based event pools in priority order
        /// 2. Resets state for the next frame
        ///
        /// This should be called from the main game loop at appropriate stages.
        /// The scheduler allows explicit control over execution order.
        /// </remarks>
        public static void UpdateComponents(
            EUpdateStage stage,
            UpdateScheduler scheduler,
            EventSubscription subManager,
            TypeChain typeChain,
            HostChain chain,
            uint mask = 0)
        {
            // Get types sorted by priority (lowest Order first)
            int[] sortedTypes = scheduler.GetSortedTypes();

            switch (stage)
            {
                case EUpdateStage.Update:
                    // Phase A: Collect events in priority order
                    for (int i = 0; i < sortedTypes.Length; i++)
                    {
                        int eventTypeId = sortedTypes[i];

                        if (ComponentRegistry.Pools[eventTypeId] is IEventDispatcher dispatcher)
                        {
                            dispatcher.SystemPoll(subManager, typeChain);
                        }
                    }

                    // Phase B: Deliver all collected events
                    EventSystem.DeliverEvents(chain);
                    break;

                case EUpdateStage.PostUpdate:
                    // Clear all frame-based event pools in priority order
                    for (int i = 0; i < sortedTypes.Length; i++)
                    {
                        int eventTypeId = sortedTypes[i];
                        ComponentRegistry.Pools[eventTypeId].ClearFramePool();
                    }
                    break;
            }
        }
    }
}using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

namespace DynamicComponent
{
    // ============================================================
    //  EVENT MANAGER — POOL FOR EVENT COMPONENTS
    // ============================================================

    /// <summary>
    /// Specialized component manager for event data (short-lived, frame-based).
    /// </summary>
    /// <typeparam name="T">Event type (must implement IEventData).</typeparam>
    /// <remarks>
    /// Events are short-lived components (typically 1 frame) used for messaging
    /// between processes. They are stored in dedicated pools and processed
    /// through EventSystem.
    ///
    /// Key differences from ComponentManager:
    /// - Events use dense allocation (no free list for roster)
    /// - Events store NamespaceMask for filtering
    /// - Events are typically cleared each frame via ClearFramePool
    ///
    /// Memory: Inherits from ComponentManager with capacity configured via MessagePoolAttribute.
    /// </remarks>
    public class EventPool<T> : ComponentPool<T>
        where T : struct, IEvent
    {
        /// <summary>
        /// Initializes a new event pool with the specified capacity.
        /// </summary>
        /// <param name="capacity">Maximum number of concurrent events.</param>
        public EventPool(int capacity)
            : base(capacity, EUpdateStage.Update, EAsyncUpdateStage.None, 0)
        {
        }

        /// <summary>
        /// Allocates a new event component.
        /// </summary>
        /// <param name="hostHandle">Host that owns this event.</param>
        /// <param name="namespaceMask">Namespace mask for subscription filtering.</param>
        /// <param name="chain">Host chain manager.</param>
        /// <returns>Handle to the allocated event.</returns>
        /// <exception cref="System.Exception">If the pool capacity is exceeded.</exception>
        /// <remarks>
        /// Events use dense allocation where rosterIndex == denseIndex.
        /// This is more efficient for frame-based events that are cleared
        /// in bulk, avoiding free list overhead.
        ///
        /// Algorithm:
        /// 1. Takes the next dense index (Partition)
        /// 2. Uses the same index as rosterIndex
        /// 3. Increments Generation, sets Host and NamespaceMask
        /// 4. Adds the event to the host's chain
        ///
        /// Complexity: O(1)
        /// </remarks>
        public Handle AllocateEvent(
            Host hostHandle,
            uint namespaceMask,
            HostChain chain)
        {
            if (Partition >= Components.Length)
                throw new System.Exception("DCS Error: Event pool capacity exceeded!");

            int denseIndex = Partition;
            int rosterIndex = denseIndex; // Events use dense indexing
            Partition++;

            // Setup roster slot
            Roster[rosterIndex].Index = denseIndex;
            Roster[rosterIndex].Generation++;
            Roster[rosterIndex].Host = hostHandle;

            int currentGen = Roster[rosterIndex].Generation;

            // Initialize event data
            ref T ev = ref Components[denseIndex];
            ev = default;
            ev.NamespaceMask = namespaceMask;

            // Set RosterIndex if the event type implements IDcsComponent
            if (ev is IComponent dcsComp)
            {
                dcsComp.RosterIndex = rosterIndex;
            }

            // Create handle and add to host chain
            Handle handle = new Handle
            {
                Id = rosterIndex,
                Generation = currentGen
            };
            chain.Add(hostHandle, handle, ComponentType<T>.Id);

            return handle;
        }

        /// <summary>
        /// Polls the event pool and builds the invocation list.
        /// </summary>
        /// <param name="subManager">Subscription manager.</param>
        /// <param name="typeChain">Type chain manager.</param>
        /// <remarks>
        /// Called by EventSystem during the Update phase.
        /// Delegates to EventSystem.PollEvents{T} which:
        /// 1. Iterates all active events in the pool
        /// 2. Finds matching subscriptions via TypeChainManager
        /// 3. Checks namespace masks
        /// 4. Builds the invocation list for EventSystem.DeliverEvents
        ///
        /// The pool knows its exact T type at compile time,
        /// enabling efficient generic dispatch.
        /// </remarks>
        public void SystemPoll(EventSubscription subManager, TypeChain typeChain)
        {
            EventSystem.PollEvents<T>(subManager, typeChain);
        }
    }
}using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

namespace DynamicComponent
{
    // ============================================================
    //  SUBSCRIPTION NODE — SUBSCRIPTION TO EVENT TYPE
    // ============================================================

    /// <summary>
    /// Subscription node linking a process (FSM) to an event type.
    /// </summary>
    /// <remarks>
    /// A subscription represents a process's interest in receiving
    /// events of a specific type, filtered by namespace mask.
    ///
    /// Stored in SubscriptionManager pool and linked via TypeChainManager
    /// for fast event dispatching.
    /// </remarks>
    public struct SubscriptionNode : IComponent
    {
        /// <summary>Event type being subscribed to (ComponentType{TEvent}.Id).</summary>
        public int TargetEventTypeId;

        /// <summary>Handle to the process (FSM) that receives the event.</summary>
        public Handle ProcessHandle;

        /// <summary>TypeId of the process pool (receiver).</summary>
        public int ProcessTypeId;

        /// <summary>Namespace mask for filtering events.</summary>
        /// <remarks>Only events with matching mask bits will be delivered.</remarks>
        public uint NamespaceMask;

        /// <summary>Index in the Roster (required by IDcsComponent).</summary>
        public int RosterIndex { get; set; }
    }

    // ============================================================
    //  SUBSCRIPTION MANAGER — REACTIVE SUBSCRIPTION MANAGEMENT
    // ============================================================

    /// <summary>
    /// Manages subscriptions for event-driven communication between processes.
    /// </summary>
    /// <remarks>
    /// Subscriptions are components stored in a dedicated pool.
    /// Each subscription links a process (FSM) to an event type.
    ///
    /// Key features:
    /// - No virtual calls or vtable in hot path
    /// - TypeChainManager handles chain linking
    /// - Subscriptions are automatically cleaned up when the process is destroyed
    ///
    /// Lifecycle:
    /// 1. AllocateSubscription: Create a new subscription
    /// 2. TypeChainManager stores the link for fast event polling
    /// 3. FreeSubscription: Remove the link and free the component
    /// </remarks>
    public class EventSubscription : ComponentPool<SubscriptionNode>
    {
        public EventSubscription(int capacity)
            : base(capacity, EUpdateStage.Update, EAsyncUpdateStage.None, 0)
        {
        }

        /// <summary>
        /// Allocates a subscription linking a process to an event type.
        /// </summary>
        /// <typeparam name="TEvent">Event type to subscribe to.</typeparam>
        /// <typeparam name="TProcess">Process type that will receive the event.</typeparam>
        /// <param name="receiverHost">Host owner of the subscription.</param>
        /// <param name="receiverProcessHandle">Handle to the process (FSM).</param>
        /// <param name="namespaceMask">Namespace mask for filtering.</param>
        /// <param name="hostChain">Host chain manager.</param>
        /// <param name="typeChain">Type chain manager.</param>
        /// <returns>Handle to the allocated subscription.</returns>
        /// <remarks>
        /// Algorithm:
        /// 1. Allocates a subscription component via base.Allocate
        /// 2. Fills the SubscriptionNode with event type, process, and mask
        /// 3. Links the subscription into the event type chain via TypeChainManager
        ///
        /// Complexity: O(1) allocation + O(1) chain insertion
        /// </remarks>
        public Handle AllocateSubscription<TEvent, TProcess>(
            Host receiverHost,
            Handle receiverProcessHandle,
            uint namespaceMask,
            HostChain hostChain,
            TypeChain typeChain)
            where TEvent : struct, IEvent
            where TProcess : struct, IComponent, IMessageReceiver
        {
            if (receiverProcessHandle.IsNull) return default;

            // 1. Allocate subscription as a component of the host
            Handle subHandle = base.Allocate(receiverHost, hostChain);

            int denseIndex = Partition - 1;
            ref SubscriptionNode node = ref Components[denseIndex];

            int eventTypeId = ComponentType<TEvent>.Id;
            node.TargetEventTypeId = eventTypeId;
            node.ProcessHandle = receiverProcessHandle;
            node.ProcessTypeId = ComponentType<TProcess>.Id;
            node.NamespaceMask = namespaceMask;

            // 2. Link the subscription into the event type chain
            typeChain.Add(eventTypeId, subHandle, node.ProcessTypeId);

            return subHandle;
        }

        /// <summary>
        /// Frees a subscription and removes it from the event type chain.
        /// </summary>
        /// <param name="hostHandle">Host owner.</param>
        /// <param name="hostChain">Host chain manager.</param>
        /// <param name="typeChain">Type chain manager.</param>
        /// <param name="handle">Handle to the subscription to free.</param>
        /// <remarks>
        /// Algorithm:
        /// 1. Removes the subscription node from TypeChainManager
        /// 2. Frees the component via base.Free (Swap-Back)
        ///
        /// The Swap-Back in base.Free automatically updates Roster indices,
        /// and TypeChainManager works with stable handles, so no manual
        /// index rebuilding is required.
        ///
        /// Complexity: O(N) where N is the number of subscriptions for this event type
        /// </remarks>
        public void FreeSubscription(
            Host hostHandle,
            HostChain hostChain,
            TypeChain typeChain,
            ref Handle handle)
        {
            int rosterIndexToDelete = handle.Id;
            int denseIndexToDelete = Roster[rosterIndexToDelete].Index;
            int eventTypeId = Components[denseIndexToDelete].TargetEventTypeId;

            // Step 1: Remove from type chain
            typeChain.Remove(eventTypeId, handle);

            // Step 2: Free the component (Swap-Back will update Roster indices)
            base.Free(hostHandle, hostChain, ref handle);

            // No manual index rebuilding needed — handles in TypeChainNode are stable!
        }

        /// <summary>
        /// Clears all frame-based subscriptions from the pool.
        /// </summary>
        /// <remarks>
        /// Called during PostUpdate to reset frame-persistent subscriptions.
        /// Type chain cleanup is delegated to the frame phase or TypeChainManager.
        /// </remarks>
        public override void ClearFramePool()
        {
            base.ClearFramePool();
            // Type chain cleanup is handled by the frame phase or TypeChainManager
        }
    }
}using System;
using System.Runtime.CompilerServices;

namespace DynamicComponent
{
    // ============================================================
    //  INVOKE RECORD — PENDING EVENT INVOCATION
    // ============================================================

    /// <summary>
    /// Record of a pending event invocation, collected during polling.
    /// </summary>
    /// <remarks>
    /// Stores all data needed to deliver an event to a receiver.
    /// Collected in Phase A (PollEvents) and executed in Phase B (DeliverEvents).
    ///
    /// This two-phase approach ensures:
    /// - No allocations during event delivery
    /// - No boxing of event data
    /// - Safe iteration over pools that may be modified during delivery
    ///
    /// Memory: 32 bytes (6 fields × 4-8 bytes)
    /// </remarks>
    public struct InvokeRecord
    {
        /// <summary>Host that owns the subscription (receiver).</summary>
        public Host ReceiverHost;

        /// <summary>Handle to the process (FSM) that will receive the event.</summary>
        public Handle ReceiverProcessHandle;

        /// <summary>TypeId of the receiver process pool.</summary>
        public int ReceiverProcessTypeId;

        /// <summary>TypeId of the event being delivered.</summary>
        public int EventTypeId;

        /// <summary>Index of the event in the dense event pool (denseIndex).</summary>
        public int ComponentId;

        /// <summary>Generation of the event for validation.</summary>
        public int ComponentGeneration;
    }

    // ============================================================
    //  EVENT SYSTEM — TWO-PHASE EVENT DISPATCHING
    // ============================================================

    /// <summary>
    /// Core event dispatching system with zero-allocation, two-phase delivery.
    /// </summary>
    /// <remarks>
    /// Phase A (PollEvents):
    /// - Collects events from all event pools
    /// - Matches them with subscriptions via TypeChainManager
    /// - Filters by namespace mask
    /// - Builds an invocation list
    ///
    /// Phase B (DeliverEvents):
    /// - Iterates the invocation list
    /// - Delivers each event to its receiver
    /// - No allocations, no boxing, no virtual calls in hot path
    ///
    /// This design is cache-friendly and GC-friendly.
    /// </remarks>
    public static class EventSystem
    {
        /// <summary>Maximum number of invocations per frame.</summary>
        public const int MaxInvokes = 1000;

        /// <summary>Pre-allocated invocation list.</summary>
        private static readonly InvokeRecord[] _invokeList = new InvokeRecord[MaxInvokes];

        /// <summary>Current number of pending invocations.</summary>
        private static int _invokeCount = 0;

        /// <summary>
        /// Phase A: Collect and filter events by namespace masks.
        /// </summary>
        /// <typeparam name="TEvent">Event type to poll.</typeparam>
        /// <param name="subManager">Subscription manager.</param>
        /// <param name="typeChain">Type chain manager.</param>
        /// <remarks>
        /// Algorithm:
        /// 1. Gets the event pool for TEvent
        /// 2. Returns early if no events are active
        /// 3. Gets the head of the subscription chain for this event type
        /// 4. For each subscription:
        ///    a. Resolves the subscription node
        ///    b. Iterates all active events in the pool
        ///    c. Checks namespace mask match: (ev.NamespaceMask & sub.NamespaceMask) != 0
        ///    d. If match, adds an InvokeRecord to the list
        /// 5. Continues to the next subscription in the chain
        ///
        /// Complexity: O(E × S) where E = events, S = subscriptions for this event type
        /// In practice, S is small per event type.
        /// </remarks>
        public static void PollEvents<TEvent>(
            EventSubscription subManager,
            TypeChain typeChain)
            where TEvent : struct, IEvent
        {
            int eventTypeId = ComponentType<TEvent>.Id;

            var eventPool = (EventPool<TEvent>)ComponentRegistry.Pools[eventTypeId];
            if (eventPool.Partition == 0) return;

            // Get the head of the subscription chain for this event type
            int chainNodeIdx = typeChain.GetTypeChainHead(eventTypeId);

            // Iterate over all subscriptions for this event type
            while (chainNodeIdx >= 0)
            {
                ref TypeChainNode chainNode = ref typeChain.GetNode(chainNodeIdx);

                // Resolve the subscription structure
                ref SubscriptionNode sub = ref subManager.ResolveHandle(chainNode.SubscriptionHandle);

                // Iterate all active events in the pool
                int partitionSnapshot = eventPool.Partition;
                for (int j = 0; j < partitionSnapshot; j++)
                {
                    ref TEvent ev = ref eventPool.Components[j];

                    // Check namespace mask match
                    if ((ev.NamespaceMask & sub.NamespaceMask) != 0)
                    {
                        if (_invokeCount >= _invokeList.Length) return;

                        _invokeList[_invokeCount] = new InvokeRecord
                        {
                            ReceiverHost = eventPool.Roster[j].Host,
                            ReceiverProcessHandle = sub.ProcessHandle,
                            ReceiverProcessTypeId = sub.ProcessTypeId,
                            EventTypeId = eventTypeId,
                            ComponentId = j, // denseIndex of the event
                            ComponentGeneration = eventPool.Roster[j].Generation
                        };
                        _invokeCount++;
                    }
                }

                // Move to the next subscription in the chain
                chainNodeIdx = chainNode.Next;
            }
        }

        /// <summary>
        /// Phase B: Deliver all collected events.
        /// </summary>
        /// <param name="chain">Host chain manager.</param>
        /// <remarks>
        /// Algorithm:
        /// 1. Iterates all records in the invocation list
        /// 2. For each record:
        ///    a. Creates a MessageHandle from the record data
        ///    b. Gets the target pool by ProcessTypeId
        ///    c. Calls SystemDeliver on the target pool
        /// 3. Clears the invocation list (sets count to 0)
        ///
        /// No allocations, no boxing, no virtual calls.
        /// The receiver implements IDcsMessageReceiver and handles the message.
        ///
        /// Complexity: O(I) where I = number of invocations
        /// </remarks>
        public static void DeliverEvents(HostChain chain)
        {
            for (int i = 0; i < _invokeCount; i++)
            {
                InvokeRecord record = _invokeList[i];

                // Create a message handle from the record
                Handle msgHandle = new Handle
                {
                    Id = record.ComponentId,
                    Generation = record.ComponentGeneration
                };

                // Get the target pool and deliver the message
                IComponentPool targetPool = ComponentRegistry.Pools[record.ReceiverProcessTypeId];
                int receiverRosterId = record.ReceiverProcessHandle.Id;

                targetPool.SystemDeliver(receiverRosterId, record.EventTypeId, msgHandle);
            }

            // Clear the invocation list for the next frame
            _invokeCount = 0;
        }
    }
}using System.Runtime.CompilerServices;
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
}using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

namespace DynamicComponent
{
    // ============================================================
    //  HOST MANAGER — COMPONENT OWNER MANAGEMENT
    // ============================================================

    /// <summary>
    /// Host manager — handles creation, validation, and destruction of Hosts.
    /// </summary>
    /// <remarks>
    /// Host is a lightweight owner of components (game object, NPC, bullet).
    ///
    /// Main functions:
    /// - Create new hosts (CreateHost)
    /// - Validate handles (IsValid)
    /// - Destroy hosts and free all components (DestroyHost)
    /// - Reuse IDs via Free List
    ///
    /// Memory: Static GlobalHosts array of MaxGameObjects elements.
    /// Each Host is 16 bytes (4 fields × 4 bytes).
    /// Total: 100,000 × 16 = 1.6 MB
    /// </remarks>
    public static class HostManager
    {
        /// <summary>Maximum number of concurrently existing hosts.</summary>
        public const int MaxGameObjects = 100000;

        /// <summary>Array of all hosts.</summary>
        /// <remarks>
        /// Array index = Host Id.
        /// Hosts are never physically removed — only invalidated via Generation.
        /// </remarks>
        public static readonly HostData[] GlobalHosts = new HostData[MaxGameObjects];

        /// <summary>Head of the free host ID list.</summary>
        /// <remarks>
        /// -1 means no free IDs available.
        /// Linked list is organized via the Host.Next field.
        /// </remarks>
        private static int _firstFree = 0;

        /// <summary>Static constructor — initializes the host array.</summary>
        /// <remarks>
        /// All hosts are created with Generation = 1 (0 means null).
        /// FirstComponent = -1 (no components).
        /// Next = i + 1 (linked list of free IDs).
        /// </remarks>
        static HostManager()
        {
            for (int i = 0; i < MaxGameObjects; i++)
            {
                GlobalHosts[i] = new HostData
                {
                    Id = i,
                    Generation = 1,
                    FirstComponent = -1,
                    Next = i + 1
                };
            }
            GlobalHosts[MaxGameObjects - 1].Next = -1;
            _firstFree = 0;
        }

        /// <summary>Creates a new host.</summary>
        /// <returns>Handle to the new host.</returns>
        /// <exception cref="System.Exception">If the host limit is exceeded.</exception>
        /// <remarks>
        /// Algorithm:
        /// 1. Takes an ID from the free list (_firstFree)
        /// 2. Increments Generation (protects against stale handles)
        /// 3. Updates _firstFree to the next free ID
        /// 4. Returns HostHandle with Id and new Generation
        ///
        /// Complexity: O(1)
        /// </remarks>
        public static Host CreateHost()
        {
            if (_firstFree < 0)
                throw new System.Exception("DCS Error: Host ID limit exceeded!");

            int id = _firstFree;
            ref HostData host = ref GlobalHosts[id];

            host.Generation++;
            _firstFree = host.Next;

            return new Host { Id = id, Generation = host.Generation };
        }

        /// <summary>Checks whether a host handle is valid.</summary>
        /// <param name="host">Handle to check.</param>
        /// <returns>True if the host exists and has not been destroyed.</returns>
        /// <remarks>
        /// Compares Generation in the handle and in the array.
        /// If values match — the host is valid.
        /// If not — the host was destroyed and the ID was reused.
        ///
        /// Complexity: O(1)
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(Host host)
        {
            return GlobalHosts[host.Id].Generation == host.Generation;
        }

        /// <summary>Invalidates a host (marks as destroyed).</summary>
        /// <param name="host">Host handle to invalidate.</param>
        /// <remarks>
        /// Algorithm:
        /// 1. Checks handle validity
        /// 2. Increments Generation (all old handles become invalid)
        /// 3. Adds the ID to the free list
        ///
        /// Note: Does NOT free components!
        /// Use DestroyHost for complete destruction.
        /// </remarks>
        public static void Invalidate(Host host)
        {
            if (!IsValid(host)) return;

            ref HostData hostRef = ref GlobalHosts[host.Id];

            hostRef.Generation++; // Protects against stale handles
            hostRef.Next = _firstFree;
            _firstFree = host.Id;
        }

        /// <summary>Completely destroys a host and all its components.</summary>
        /// <param name="host">Host handle to destroy.</param>
        /// <param name="chainManager">Component chain manager.</param>
        /// <remarks>
        /// Algorithm:
        /// 1. Checks host validity
        /// 2. Frees all components via DynamicComponentSystem.FreeChain
        /// 3. Invalidates the host (frees the ID)
        ///
        /// This is the preferred way to destroy a game object,
        /// as it guarantees all resources are freed.
        /// </remarks>
        public static void DestroyHost(Host host, HostChain chainManager)
        {
            if (!IsValid(host)) return;

            // 1. Free all components
            DCS.FreeChain(host, chainManager);

            // 2. Free the host itself
            Invalidate(host);
        }
    }
}using System.Runtime.CompilerServices;

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
                _nodes[i].SubscriptionHandle.Id = -1;
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
}﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DynamicComponent
{
    /// <summary>
    /// Manages update order priorities for component types.
    /// </summary>
    /// <remarks>
    /// Provides O(1) order lookup and lazy sorting with dirty flag.
    /// Zero allocations after initialization (no LINQ allocations in hot path).
    ///
    /// Default behavior:
    /// - Types without explicit order get Order = 0
    /// - Types with Order < 0 execute before default group
    /// - Types with Order > 0 execute after default group
    /// - Sorting is lazy: only happens when SetOrder is called
    ///
    /// Thread Safety: Not thread-safe. All operations must be on main thread.
    /// </remarks>
    public sealed class UpdateScheduler
    {
        // ============================================================
        //  STATE
        // ============================================================

        /// <summary>Map of type ID → order value. Types not present default to 0.</summary>
        private readonly Dictionary<int, int> _orders = new();

        /// <summary>Sorted array of type IDs. Cached until order changes.</summary>
        private int[] _sortedTypeIds = Array.Empty<int>();

        /// <summary>Indicates whether _sortedTypeIds needs to be rebuilt.</summary>
        private bool _isDirty = true;

        /// <summary>Cache of the last count of PollTypeIds to detect registry changes.</summary>
        private int _lastPollTypeCount = -1;

        // ============================================================
        //  PUBLIC API
        // ============================================================

        /// <summary>
        /// Sets the update order priority for a component type.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="order">Order value (lower = earlier execution).</param>
        /// <remarks>
        /// Mark the cache as dirty. Next call to GetSortedTypes will rebuild.
        /// Complexity: O(1)
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetOrder<T>(int order) where T : struct
        {
            int typeId = ComponentType<T>.Id;
            _orders[typeId] = order;
            _isDirty = true;
        }

        /// <summary>
        /// Gets all component type IDs sorted by update order.
        /// </summary>
        /// <returns>Sorted array of type IDs.</returns>
        /// <remarks>
        /// Uses lazy rebuild: only sorts when order changed or registry changed.
        /// Zero allocations in hot path (returns cached array).
        ///
        /// Types without explicit order default to 0.
        /// Types with equal order maintain stable order (not guaranteed, but stable).
        ///
        /// Complexity: O(N log N) on rebuild, O(1) on cache hit.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] GetSortedTypes()
        {
            // Check if registry size changed (new types added)
            int currentPollCount = ComponentRegistry.PollTypesCount;
            if (currentPollCount != _lastPollTypeCount)
            {
                _isDirty = true;
                _lastPollTypeCount = currentPollCount;
            }

            // Rebuild if dirty
            if (_isDirty)
            {
                Rebuild();
                _isDirty = false;
            }

            return _sortedTypeIds;
        }

        /// <summary>
        /// Gets the order value for a specific type.
        /// </summary>
        /// <param name="typeId">Component type ID.</param>
        /// <returns>Order value, or 0 if not set.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetOrder(int typeId)
        {
            return _orders.GetValueOrDefault(typeId, 0);
        }

        /// <summary>
        /// Removes a type from the order map (resets to default 0).
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveOrder<T>() where T : struct
        {
            int typeId = ComponentType<T>.Id;
            if (_orders.Remove(typeId))
            {
                _isDirty = true;
            }
        }

        /// <summary>
        /// Clears all order values and resets to default (all types = 0).
        /// </summary>
        public void ClearAllOrders()
        {
            if (_orders.Count > 0)
            {
                _orders.Clear();
                _isDirty = true;
            }
        }

        /// <summary>
        /// Forces a rebuild of the sorted cache.
        /// </summary>
        /// <remarks>
        /// Useful if the registry changed externally and you want to ensure
        /// the sorted list is up to date without waiting for lazy rebuild.
        /// </remarks>
        public void ForceRebuild()
        {
            _isDirty = true;
            Rebuild();
            _isDirty = false;
        }

        // ============================================================
        //  INTERNAL
        // ============================================================

        /// <summary>
        /// Rebuilds the sorted type ID array from the registry and order map.
        /// </summary>
        /// <remarks>
        /// Uses List<T> for sorting to avoid LINQ allocations.
        /// Zero allocations on cache hit, minimal on rebuild.
        /// </remarks>
        private void Rebuild()
        {
            int count = ComponentRegistry.PollTypesCount;
            if (count == 0)
            {
                _sortedTypeIds = Array.Empty<int>();
                return;
            }

            // Copy PollTypeIds to a local list for sorting
            // This avoids LINQ allocations and gives us direct array access
            var typeIds = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                typeIds.Add(ComponentRegistry.PollTypeIds[i]);
            }

            // Sort by order (lower = first), with default 0
            typeIds.Sort((a, b) =>
            {
                int orderA = _orders.GetValueOrDefault(a, 0);
                int orderB = _orders.GetValueOrDefault(b, 0);
                return orderA.CompareTo(orderB);
            });

            // Convert to array
            _sortedTypeIds = typeIds.ToArray();

            // Update cache
            _lastPollTypeCount = count;
        }
    }
}