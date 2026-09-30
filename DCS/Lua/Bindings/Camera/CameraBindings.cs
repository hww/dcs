using DCS.Core;
using System;
using System.Runtime.InteropServices;
using AOT;

namespace DCS.Lua
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
            var args = new ArgReader(L, "Camera.Find");
            args.ExpectExactly(2);

            args.CheckInteger(1);
            string name = args.CheckString(2);
            if (string.IsNullOrEmpty(name))
                LuaFail.Fail(L, "Camera.Find", "argument #2: camera name must not be empty");

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
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
            var args = new ArgReader(L, "Camera.GetMain");
            args.ExpectExactly(1);

            args.CheckInteger(1);

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