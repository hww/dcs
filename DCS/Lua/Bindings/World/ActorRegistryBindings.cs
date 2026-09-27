using DCS.Core;
using DCS.Spatial;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DCS.Lua.Bindings
{
    /// <summary>
    /// Semantic search over Hosts by name / tag / type.
    /// Backed by NameIndex, TagIndex, and HostManager.
    /// Returns Host packed ints, not ActorHandles.
    /// </summary>
    public static class ActorRegistryBindings
    {
        private static readonly List<ushort> _scratch = new List<ushort>(128);

        public static void Register(IntPtr L)
        {
            LuaBindings.RegisterNamespace(L, "ActorRegistry", (state, tableIndex) =>
            {
                LuaBindings.RegisterMethod(state, Lua_Count, tableIndex, "Count");
                LuaBindings.RegisterMethod(state, Lua_GetByName, tableIndex, "GetByName");
                LuaBindings.RegisterMethod(state, Lua_GetByTag, tableIndex, "GetByTag");
                LuaBindings.RegisterMethod(state, Lua_IsValid, tableIndex, "IsValid");
            });
        }

        // ------------------------------------------------------------
        // ActorRegistry.Count() -> int
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Count(IntPtr L)
        {
            int count = ActorRegistryFacade.Names != null
                ? ActorRegistryFacade.Names.Count
                : 0;
            LuaNative.lua_pushinteger(L, count);
            return 1;
        }

        // ------------------------------------------------------------
        // ActorRegistry.GetByName(name) -> hostPacked or nil
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetByName(IntPtr L)
        {
            string name = LuaArgumentReader.ReadString(L, 1);
            if (string.IsNullOrEmpty(name) ||
                !ActorRegistryFacade.TryGetByName(name, out Host host))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }
            LuaNative.lua_pushinteger(L, host.ToLua());
            return 1;
        }

        // ------------------------------------------------------------
        // ActorRegistry.GetByTag(bit) -> array of hostPacked
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetByTag(IntPtr L)
        {
            int bit = LuaArgumentReader.ReadInt(L, 1);
            _scratch.Clear();
            ActorRegistryFacade.GetByTag(bit, _scratch);
            PushHostArray(L, _scratch);
            return 1;
        }

        // ------------------------------------------------------------
        // ActorRegistry.IsValid(hostPacked) -> bool
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_IsValid(IntPtr L)
        {
            int packed = LuaArgumentReader.ReadInt(L, 1);
            if (packed <= 0)
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }
            Host host = Host.FromLua(packed);
            LuaNative.lua_pushboolean(L, HostManager.IsValid(host) ? 1 : 0);
            return 1;
        }

        private static void PushHostArray(IntPtr L, List<ushort> hosts)
        {
            LuaNative.lua_newtable(L);
            for (int i = 0; i < hosts.Count; i++)
            {
                ushort id = hosts[i];
                if (id >= HostManager.MaxGameObjects) continue;
                Host h = new Host
                {
                    Id = id,
                    Generation = HostManager.GlobalHosts[id].Generation
                };
                LuaNative.lua_pushinteger(L, i + 1);
                LuaNative.lua_pushinteger(L, h.ToLua());
                LuaNative.lua_settable(L, -3);
            }
        }
    }
}