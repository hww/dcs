using System;
using System.Runtime.CompilerServices;

namespace DCS.Core
{
    /// <summary>
    /// Direct hostId -> denseIndex table for a single component type.
    /// Replaces Roster for state components (one component of type T per Host).
    /// Value is -1 when the Host has no component of this type.
    /// </summary>
    public sealed class ComponentSparse
    {
        public const int INVALID = -1;
        private readonly int[] _map;

        public ComponentSparse(int capacity)
        {
            _map = new int[capacity];
            for (int i = 0; i < capacity; i++) _map[i] = INVALID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get(int hostId)
        {
            if ((uint)hostId >= (uint)_map.Length) return INVALID;
            return _map[hostId];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int hostId, int denseIndex)
        {
            if ((uint)hostId >= (uint)_map.Length) return;
            _map[hostId] = denseIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(int hostId)
        {
            if ((uint)hostId >= (uint)_map.Length) return;
            _map[hostId] = INVALID;
        }

        public void ClearAll()
        {
            for (int i = 0; i < _map.Length; i++) _map[i] = INVALID;
        }
    }
}