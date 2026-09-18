using System;
using System.Runtime.InteropServices;
using DCS.Core;

namespace DCS.Lua.Bindings
{
    /// <summary>
    /// Lua-домен "Camera": только поиск Host камеры по имени.
    /// Yaw/Pitch/позиция — через DCS.GetField/SetField на компонентах.
    ///
    /// Использование из Lua:
    ///   local camHost = Camera.Find("MainCamera")
    ///   local lookComp = DCS.GetComponent(COMPONENT.CameraLookComponent, camHost)
    ///   local yaw = DCS.GetField(COMPONENT.CameraLookComponent, lookComp, "Yaw")
    /// </summary>
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
        //  Camera.Find(name) -> packedHost | nil
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Find(IntPtr L)
        {
            string name = ReadString(L, 1);
            if (string.IsNullOrEmpty(name))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            if (CameraSystem.TryFindByName(name, LuaManager._globalHostChain, out Host host))
            {
                LuaNative.lua_pushinteger(L, host.ToLua());
                return 1;
            }

            LuaNative.lua_pushnil(L);
            return 1;
        }

        // ------------------------------------------------------------
        //  Camera.GetMain() -> packedHost | nil
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetMain(IntPtr L)
        {
            if (CameraSystem.TryFindMain(LuaManager._globalHostChain, out Host host))
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