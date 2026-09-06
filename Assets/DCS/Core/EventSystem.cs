using System;
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
                    Id = (ushort)record.ComponentId,
                    Generation = (ushort)record.ComponentGeneration
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
}