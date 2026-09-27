using System;
using System.Runtime.CompilerServices;

namespace DCS.Core
{
    /// <summary>
    /// Shared sparse table across several FastPool<T> instances.
    /// When two or more pools share the same FastSparseTable, their
    /// dense indices for a given hostId are guaranteed to be identical.
    /// This is the "group" mechanism.
    /// </summary>
    public sealed class FastSparseTable
    {
        public const int INVALID = -1;

        // hostId -> denseIndex
        private readonly int[] _map;

        // denseIndex -> hostId (needed for swap-back on free)
        private readonly ushort[] _owner;

        public FastSparseTable(int hostCapacity)
        {
            _map = new int[hostCapacity];
            _owner = new ushort[hostCapacity];
            for (int i = 0; i < hostCapacity; i++) _map[i] = INVALID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetDense(int hostId)
        {
            if ((uint)hostId >= (uint)_map.Length) return INVALID;
            return _map[hostId];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDense(int hostId, int denseIndex)
        {
            _map[hostId] = denseIndex;
            _owner[denseIndex] = (ushort)hostId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearDense(int hostId)
        {
            _map[hostId] = INVALID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetOwner(int denseIndex)
        {
            return _owner[denseIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveOwner(int fromDense, int toDense)
        {
            _owner[toDense] = _owner[fromDense];
        }

        public void ClearAll()
        {
            for (int i = 0; i < _map.Length; i++) _map[i] = INVALID;
        }
    }
}