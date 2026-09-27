using DCS.Actors;
using DCS.Core;

namespace DCS.Spatial
{
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
}