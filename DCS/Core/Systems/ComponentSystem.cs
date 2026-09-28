using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

namespace DCS.Core
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
    public static class DCSystem
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
        public static Handle Get<T>(Host hostHandle, HostChain chain) where T : struct, IComponent
        {
            ChainNode typed = chain.GetTypedHandle(hostHandle, ComponentType<T>.Id);
            if (typed.Component.Id == 0 && typed.Component.Generation == 0)
                return Handle.Null;
            return typed.Component;
        }

        /// <summary>
        /// Allocates a new component of type T and adds it to the host's chain.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="hostHandle">Host that owns the component.</param>
        /// <param name="chain">Host chain manager.</param>
        /// <returns>Handle to the allocated component.</returns>
        public static Handle Allocate<T>(Host hostHandle, HostChain chain) where T : struct, IComponent
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
        public static ref T ResolveHandle<T>(Handle handle) where T : struct, IComponent
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
        public static void Free<T>(Host hostHandle, HostChain chain, ref Handle handle) where T : struct, IComponent
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
    }
}