using System.Runtime.CompilerServices;
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
                Id = (ushort)rosterIndex,
                Generation = (ushort)currentGen
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
}