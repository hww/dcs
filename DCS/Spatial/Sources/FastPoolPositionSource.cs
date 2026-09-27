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
                action(host, pos.Value);
            }
        }

        public bool TryGet(Host host, out Vector3 value)
        {
            if (_pool.TryResolve(host, out int d)) { value = _pool.Components[d].Value; return true; }
            value = default; return false;
        }
    }

    public sealed class FastPoolNameSource : INameSource
    {
        private readonly FastPool<NameComponent> _pool;
        public FastPoolNameSource(FastPool<NameComponent> pool) { _pool = pool; }

        public bool TryGet(Host host, out string value)
        {
            if (_pool.TryResolve(host, out int d)) { value = _pool.Components[d].Name; return true; }
            value = null; return false;
        }
    }

    public sealed class FastPoolTagSource : ITagSource
    {
        private readonly FastPool<TagComponent> _pool;
        public FastPoolTagSource(FastPool<TagComponent> pool) { _pool = pool; }

        public bool TryGet(Host host, out uint value)
        {
            if (_pool.TryResolve(host, out int d)) { value = _pool.Components[d].Mask; return true; }
            value = 0; return false;
        }
    }
}