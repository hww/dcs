using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DCS.Core;
using DCS.Spatial;

namespace DCS.Lua
{
    public static class ActorRegistryBindings
    {
        private static readonly List<Host> _scratch = new List<Host>(128);

        public static void Register(IntPtr L)
        {
            LuaBindings.RegisterNamespace(L, "ActorRegistry", (state, tableIndex) =>
            {
                LuaBindings.RegisterMethod(state, Lua_Count, tableIndex, "Count");
                LuaBindings.RegisterMethod(state, Lua_GetByName, tableIndex, "GetByName");
                LuaBindings.RegisterMethod(state, Lua_GetByArchetype, tableIndex, "GetByArchetype");
                LuaBindings.RegisterMethod(state, Lua_IsValid, tableIndex, "IsValid");
            });
        }

        private static bool TryGetSpatialDomain(IntPtr L, int argIndex, out SpatialDomain spatialDomain)
        {
            spatialDomain = null;
            if (!HostResolver.TryGetDomain(L, argIndex, out Domain domain))
                return false;
            if (!domain.HasSpatial)
                return false;
            return SpatialDomainRegistry.TryGet(domain.SpatialDomainId, out spatialDomain);
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Count(IntPtr L)
        {
            var args = new ArgReader(L, "ActorRegistry.Count");
            args.ExpectExactly(1);
            args.CheckInteger(1);

            if (!TryGetSpatialDomain(L, 1, out var sd))
            {
                LuaNative.lua_pushinteger(L, 0);
                return 1;
            }
            LuaNative.lua_pushinteger(L, sd.Names.Count);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetByName(IntPtr L)
        {
            var args = new ArgReader(L, "ActorRegistry.GetByName");
            args.ExpectExactly(2);
            args.CheckInteger(1);
            string name = args.CheckString(2);

            if (string.IsNullOrEmpty(name))
                LuaFail.Fail(L, "ActorRegistry.GetByName", "argument #2: name must not be empty");

            if (!TryGetSpatialDomain(L, 1, out var sd) ||
                !sd.Names.TryGet(new Name(name), out ushort hostId))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            if (hostId >= HostManager.MaxGameObjects)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            Host host = new Host
            {
                Id = hostId,
                Generation = HostManager.GlobalHosts[hostId].Generation
            };
            if (!HostManager.IsValid(host))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            LuaNative.lua_pushinteger(L, host.ToLua());
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetByArchetype(IntPtr L)
        {
            var args = new ArgReader(L, "ActorRegistry.GetByArchetype");
            args.ExpectExactly(2);
            args.CheckInteger(1);
            string archetype = args.CheckString(2);

            if (string.IsNullOrEmpty(archetype))
                LuaFail.Fail(L, "ActorRegistry.GetByArchetype",
                    "argument #2: archetype must not be empty");

            if (!TryGetSpatialDomain(L, 1, out var sd))
            {
                LuaNative.lua_newtable(L);
                return 1;
            }

            uint nameId = Crc32.Get(archetype);
            _scratch.Clear();
            sd.Archetypes.GetAllBy(nameId, _scratch);
            PushHostArray(L, _scratch);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_IsValid(IntPtr L)
        {
            var args = new ArgReader(L, "ActorRegistry.IsValid");
            args.ExpectExactly(1);
            int packed = (int)args.CheckInteger(1);

            if (packed <= 0)
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            Host host = Host.FromLua(packed);
            LuaNative.lua_pushboolean(L, HostManager.IsValid(host) ? 1 : 0);
            return 1;
        }

        private static void PushHostArray(IntPtr L, List<Host> hosts)
        {
            LuaNative.lua_newtable(L);
            for (int i = 0; i < hosts.Count; i++)
            {
                ushort id = hosts[i].Id;
                if (id >= HostManager.MaxGameObjects) continue;

                Host h = new Host
                {
                    Id = id,
                    Generation = HostManager.GlobalHosts[id].Generation
                };
                if (!HostManager.IsValid(h)) continue;

                LuaNative.lua_pushinteger(L, i + 1);
                LuaNative.lua_pushinteger(L, h.ToLua());
                LuaNative.lua_settable(L, -3);
            }
        }
    }
}