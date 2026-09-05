using System.Runtime.CompilerServices;
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
}