using DCS.Core;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DCS.Lua.Bindings
{
    public static class HostBindings
    {
        public static void Register(IntPtr L, int tableIndex)
        {
            LuaBindings.RegisterMethod(L, Lua_CreateHost, tableIndex, "CreateHost");
            LuaBindings.RegisterMethod(L, Lua_Attach, tableIndex, "Attach");
            LuaBindings.RegisterMethod(L, Lua_Spawn, tableIndex, "Spawn");
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_CreateHost(IntPtr L)
        {
            Host host = HostManager.CreateHost();
            LuaNative.lua_pushinteger(L, host.ToLua());
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Attach(IntPtr L)
        {
            int packedHost = LuaArgumentReader.ReadInt(L, 1);
            string goName = LuaArgumentReader.ReadString(L, 2);

            Host host = Host.FromLua(packedHost);
            if (!HostManager.IsValid(host) || string.IsNullOrEmpty(goName))
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            bool ok = ViewService.Attach(host, goName);
            LuaNative.lua_pushboolean(L, ok ? 1 : 0);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Spawn(IntPtr L)
        {
            string prefabPath = LuaArgumentReader.ReadString(L, 1);
            string goName = LuaArgumentReader.ReadString(L, 2);

            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            GameObject go = UnityEngine.Object.Instantiate(prefab);
            go.name = goName;

            Host host = HostManager.CreateHost();

            var link = go.GetComponent<IHostReference>();
            if (link == null)
            {
                UnityEngine.Object.Destroy(go);
                LuaNative.lua_pushnil(L);
                return 1;
            }

            HostManager.LinkHostReference(host, link);
            LuaNative.lua_pushinteger(L, host.ToLua());
            return 1;
        }
    }
}