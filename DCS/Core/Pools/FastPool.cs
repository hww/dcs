using System;
using System.Runtime.CompilerServices;

namespace DCS.Core
{
    /// <summary>
    /// Fast, group-friendly pool for state components.
    /// One component of type T per Host. No Generation, no Handle-based indirection.
    ///
    /// If multiple FastPool instances share the same FastSparseTable,
    /// then for any host that has all of them, their dense indices match.
    /// </summary>
    public class FastPool<T> where T : struct, IComponent
    {
        public T[] Components;
        public int Partition;

        private readonly FastSparseTable _table;
        private readonly int _poolId;

        public FastSparseTable Table => _table;
        public int PoolId => _poolId;
        public Type ComponentType => typeof(T);

        public FastPool(FastSparseTable table, int capacity, int poolId)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            Components = new T[capacity];
            Partition = 0;
            _poolId = poolId;
        }

        /// <summary>
        /// Allocate (or return existing) component for this Host.
        /// Returns true if the Host already had one.
        /// </summary>
        public bool TryAllocate(Host host, out bool alreadyExisted)
        {
            alreadyExisted = false;
            if (!HostManager.IsValid(host)) return false;

            int existing = _table.GetDense(host.Id);
            if (existing != FastSparseTable.INVALID)
            {
                alreadyExisted = true;
                return true;
            }

            if (Partition >= Components.Length)
                throw new Exception(
                    $"DCS Error: FastPool capacity exceeded for {typeof(T).Name}!");

            int dense = Partition++;
            ref T c = ref Components[dense];
            c = default;
            c.RosterIndex = host.Id; // in FastPool, RosterIndex == hostId

            _table.SetDense(host.Id, dense);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryResolve(Host host, out int denseIndex)
        {
            denseIndex = _table.GetDense(host.Id);
            return denseIndex != FastSparseTable.INVALID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Resolve(Host host)
        {
            int dense = _table.GetDense(host.Id);
            return ref Components[dense];
        }

        /// <summary>
        /// Free the component for this Host. Swap-back compaction.
        /// </summary>
        public void Free(Host host)
        {
            int denseToDelete = _table.GetDense(host.Id);
            if (denseToDelete == FastSparseTable.INVALID) return;

            _table.ClearDense(host.Id);
            Partition--;

            int denseToMove = Partition;
            if (denseToDelete != denseToMove)
            {
                Components[denseToDelete] = Components[denseToMove];
                ushort movedHostId = _table.GetOwner(denseToMove);
                _table.SetDense(movedHostId, denseToDelete);
            }
        }

        public void Clear()
        {
            Partition = 0;
            _table.ClearAll();
        }
    }
}