using System;
using System.Runtime.InteropServices;
using DCS.Core;

namespace DCS.Lua.Bindings
{
    public static class CameraBindings
    {
        public static void Register(IntPtr L)
        {
            LuaNative.lua_newtable(L);
            LuaBindings.RegisterMethod(L, Lua_Find, "Find");
            LuaBindings.RegisterMethod(L, Lua_GetMain, "GetMain");
            LuaNative.lua_setglobal(L, "Camera");
        }

        // ------------------------------------------------------------
        //  Camera.Find(chainId, name) -> packedHost | nil
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Find(IntPtr L)
        {
            int chainId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            string name = ReadString(L, 2);

            if (string.IsNullOrEmpty(name))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            HostChain chain = DomainRegistry.Get(chainId).HostChain;
            if (chain == null)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            if (CameraSystem.TryFindByName(name, chain, out Host host))
            {
                LuaNative.lua_pushinteger(L, host.ToLua());
                return 1;
            }

            LuaNative.lua_pushnil(L);
            return 1;
        }

        // ------------------------------------------------------------
        //  Camera.GetMain(chainId) -> packedHost | nil
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetMain(IntPtr L)
        {
            int chainId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);

            HostChain chain = DomainRegistry.Get(chainId).HostChain;
            if (chain == null)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            if (CameraSystem.TryFindMain(chain, out Host host))
            {
                LuaNative.lua_pushinteger(L, host.ToLua());
                return 1;
            }

            LuaNative.lua_pushnil(L);
            return 1;
        }

        private static string ReadString(IntPtr L, int index)
        {
            IntPtr ptr = LuaNative.lua_tolstring(L, index, IntPtr.Zero);
            return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : null;
        }
    }
}