using System.Runtime.CompilerServices;
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
}