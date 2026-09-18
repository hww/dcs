using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DCS.Lua.Bindings
{
    public static class InputBindings
    {
        public static void Register(IntPtr L)
        {
            // --- Input.GetKey(int) / Input.GetKeyDown(int) ---
            LuaNative.lua_newtable(L);
            LuaBindings.RegisterMethod(L, Lua_GetKey, "GetKey");
            LuaBindings.RegisterMethod(L, Lua_GetKeyDown, "GetKeyDown");
            LuaBindings.RegisterMethod(L, Lua_GetAxis, "GetAxis");
            LuaNative.lua_setglobal(L, "Input");

            // --- KeyCode.W, KeyCode.Space, ... ---
            LuaNative.lua_newtable(L);
            foreach (KeyCode code in Enum.GetValues(typeof(KeyCode)))
            {
                LuaNative.lua_pushinteger(L, (int)code);
                LuaNative.lua_setfield(L, -2, code.ToString());
            }
            LuaNative.lua_setglobal(L, "KeyCode");
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetKey(IntPtr L)
        {
            int keyCodeInt = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            KeyCode code = (KeyCode)keyCodeInt;
            bool pressed = UnityEngine.Input.GetKey(code);
            LuaNative.lua_pushboolean(L, pressed ? 1 : 0);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetKeyDown(IntPtr L)
        {
            int keyCodeInt = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            KeyCode code = (KeyCode)keyCodeInt;
            bool pressed = UnityEngine.Input.GetKeyDown(code);
            LuaNative.lua_pushboolean(L, pressed ? 1 : 0);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetAxis(IntPtr L)
        {
            string axis = ReadString(L, 1);
            float v = string.IsNullOrEmpty(axis) ? 0f : UnityEngine.Input.GetAxis(axis);
            LuaNative.lua_pushnumber(L, v);
            return 1;
        }

        private static string ReadString(IntPtr L, int index)
        {
            IntPtr ptr = LuaNative.lua_tolstring(L, index, IntPtr.Zero);
            return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : null;
        }
    }
}