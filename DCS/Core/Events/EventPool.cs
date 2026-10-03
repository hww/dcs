using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

namespace DCS.Core
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
    public class EventPool<T> : ComponentPool<T>, IEventPool
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

        public int EventPartition => Partition;
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

            System.UInt16 denseIndex = Partition;
            System.UInt16 rosterIndex = denseIndex; // Events use dense indexing
            Partition++;

            // Setup roster slot
            Roster[rosterIndex].Index = (System.UInt16)denseIndex;
            Roster[rosterIndex].Generation++;

            int currentGen = Roster[rosterIndex].Generation;

            // Initialize event data
            ref T ev = ref Components[denseIndex];
            ev = default;
            ev.NamespaceMask = namespaceMask;
            ev.Host = hostHandle;

            // Create handle and add to host chain
            Handle handle = new Handle
            {
                Id = (ushort)rosterIndex,
                Generation = (ushort)currentGen
            };
            chain.Add(hostHandle, handle, ComponentType<T>.Id);

            return handle;
        }

        public void GetMessageHandle(int denseIndex, out Handle handle)
        {
            // У EventPool denseIndex == rosterIndex.
            int rosterIdx = denseIndex;
            handle = new Handle
            {
                Id = (ushort)rosterIdx,
                Generation = (ushort)Roster[rosterIdx].Generation
            };
        }

        public uint GetMessageNamespaceMask(int denseIndex)
        {
            return Components[denseIndex].NamespaceMask;
        }

        public Host GetSenderHost(int denseIndex)
        {
            return Components[denseIndex].Host;
        }

    }
}