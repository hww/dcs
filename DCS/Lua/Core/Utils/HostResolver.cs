using DCS.Core;
using System;

namespace DCS.Lua
{
    /// <summary>
    /// Centralized host and domain lookup helpers shared by all bindings.
    /// </summary>
    public static class HostResolver
    {
        public static bool TryGetDomain(IntPtr L, int argIndex, out Domain domain)
        {
            int domainId = LuaArgumentReader.ReadInt(L, argIndex);
            domain = DomainRegistry.Get(domainId) ?? DomainRegistry.Get(Domain.DefaultId);
            return domain != null;
        }

        public static bool TryGetHost(int hostId, out Host host)
        {
            host = default;
            if (hostId < 0 || hostId >= HostManager.MaxGameObjects)
                return false;

            ref HostData data = ref HostManager.GlobalHosts[hostId];
            host = new Host { Id = (ushort)hostId, Generation = data.Generation };
            return HostManager.IsValid(host);
        }

        public static IHostReference ResolveReference(IntPtr L, int index)
        {
            int packed = LuaArgumentReader.ReadInt(L, index);
            if (packed == HandleConfig.NULL_INDEX)
                return null;
            return HostManager.GetHostReference(new Handle(packed));
        }
    }
}