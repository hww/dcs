using DCS.Actors;
using DCS.Core;

namespace DCS.Spatial
{
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