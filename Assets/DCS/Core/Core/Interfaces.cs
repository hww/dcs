using System;

namespace DynamicComponent
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
        /// Allows non-generic allocation via the native Lua bridge
        /// </summary>
        /// <param name="hostHandle"></param>
        /// <param name="chain"></param>
        /// <returns></returns>
        Handle SystemAllocate(Host hostHandle, HostChain chain);

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

        /// <summary>
        /// Tries to get the dense index from a Handle.
        /// Validates the Handle's Generation against the Roster slot.
        /// </summary>
        bool TryGetDenseIndex(Handle handle, out int denseIndex);

        /// <summary>
        /// Reads a field from a component and pushes it to Lua stack.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        void GetField(int denseIndex, string fieldName, IntPtr L);

        /// <summary>
        /// Reads a value from Lua stack and writes it to a component field.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        void SetField(int denseIndex, string fieldName, IntPtr L);
        /// <summary>
        /// Tries to get the Host that owns the component identified by the given Handle.
        /// Validates the Handle's Generation against the Roster slot.
        /// </summary>
        bool TryGetHost(Handle handle, out Host host);

    }

}