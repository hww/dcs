using DCS.Core;
using System;
using System.Runtime.InteropServices;

namespace DCS.Lua.Bindings
{
    public static class CameraBindings
    {
        public static void Register(IntPtr L)
        {
            LuaBindings.RegisterNamespace(L, "Camera", (state, tableIndex) =>
            {
                LuaBindings.RegisterMethod(state, Lua_Find, tableIndex, "Find");
                LuaBindings.RegisterMethod(state, Lua_GetMain, tableIndex, "GetMain");
            });
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Find(IntPtr L)
        {
            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            string name = LuaArgumentReader.ReadString(L, 2);
            if (string.IsNullOrEmpty(name))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            if (CameraSystem.TryFindByName(name, domain.HostChain, out Host host))
            {
                LuaNative.lua_pushinteger(L, host.ToLua());
                return 1;
            }

            LuaNative.lua_pushnil(L);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetMain(IntPtr L)
        {
            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            if (CameraSystem.TryFindMain(domain.HostChain, out Host host))
            {
                LuaNative.lua_pushinteger(L, host.ToLua());
                return 1;
            }

            LuaNative.lua_pushnil(L);
            return 1;
        }
    }
}