using System;
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