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
}