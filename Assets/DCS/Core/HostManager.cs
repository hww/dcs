using System.Runtime.CompilerServices;
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
        public const int MaxGameObjects = 65535;

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
            for (ushort i = 0; i < MaxGameObjects; i++)
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

            return new Host { Id = (ushort)id, Generation = (ushort)host.Generation };
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
}