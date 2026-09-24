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
        //  Camera.Find(domainId, name) -> packedHost | nil
        //  Запрос: «нет камеры с таким именем» — валидный nil.
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Find(IntPtr L)
        {
            int domainId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            string name = ReadString(L, 2);

            if (string.IsNullOrEmpty(name))
                return LuaNative.lua_error(L,
                    "[CameraBindings] Find: name is empty");

            var domain = DomainRegistry.Get(domainId);
            if (domain == null)
                return LuaNative.lua_error(L,
                    $"[CameraBindings] Find: domain id {domainId} not found");

            HostChain chain = domain.HostChain;
            if (chain == null)
                return LuaNative.lua_error(L,
                    $"[CameraBindings] Find: domain {domainId} has no HostChain");

            if (CameraSystem.TryFindByName(name, chain, out Host host))
            {
                LuaNative.lua_pushinteger(L, host.ToLua());
                return 1;
            }

            LuaNative.lua_pushnil(L);
            return 1;
        }

        // ------------------------------------------------------------
        //  Camera.GetMain(domainId) -> packedHost | nil
        //  Запрос: «нет главной камеры» — валидный nil.
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetMain(IntPtr L)
        {
            int domainId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);

            var domain = DomainRegistry.Get(domainId);
            if (domain == null)
                return LuaNative.lua_error(L,
                    $"[CameraBindings] GetMain: domain id {domainId} not found");

            HostChain chain = domain.HostChain;
            if (chain == null)
                return LuaNative.lua_error(L,
                    $"[CameraBindings] GetMain: domain {domainId} has no HostChain");

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