using DCS.Actors;
using DCS.Core;
using UnityEngine;

namespace DCS.Spatial
{
    public sealed class FastPoolPositionSource : IPositionSource
    {
        private readonly FastPool<PositionComponent> _pool;
        public FastPoolPositionSource(FastPool<PositionComponent> pool) { _pool = pool; }

        public void ForEach(System.Action<Host, Vector3> action)
        {
            for (int i = 0; i < _pool.Partition; i++)
            {
                ref var pos = ref _pool.Components[i];
                ushort hostId = (ushort)pos.RosterIndex;
                Host host = new Host { Id = hostId, Generation = HostManager.GlobalHosts[hostId].Generation };
                action(host, pos.Position);
            }
        }

        public bool TryGet(Host host, out Vector3 value)
        {
            if (_pool.TryResolve(host, out int d)) { value = _pool.Components[d].Position; return true; }
            value = default; return false;
        }
    }
}