using System;
using System.Runtime.InteropServices;
using AOT;

namespace DCS.Lua.Bindings
{
    /// <summary>
    /// Биндинги для инфраструктуры Lua-runtime.
    /// Не относятся к DCS, не относятся к игре.
    /// </summary>
    public static class LuaRuntimeBindings
    {
        public static void Register(IntPtr L)
        {
            LuaBindings.RegisterGlobalFunction(L, Lua_ReadScriptFile, "Internal_ReadScriptFile");
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_ReadScriptFile(IntPtr L)
        {
            string path = ReadString(L, 1);
            if (string.IsNullOrEmpty(path))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            if (LuaManager.Instance == null)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            string code = LuaManager.Instance.ReadScriptFile(path);
            if (string.IsNullOrEmpty(code))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            LuaNative.lua_pushstring(L, code);
            return 1;
        }

        private static string ReadString(IntPtr L, int index)
        {
            IntPtr ptr = LuaNative.lua_tolstring(L, index, IntPtr.Zero);
            return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : null;
        }
    }
}