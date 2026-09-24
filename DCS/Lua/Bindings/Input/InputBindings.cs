using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DCS.Lua.Bindings
{
    public static class InputBindings
    {
        private static readonly HashSet<int> ValidKeyCodes = BuildValidKeyCodes();

        private static HashSet<int> BuildValidKeyCodes()
        {
            var set = new HashSet<int>();
            foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
                set.Add((int)k);
            return set;
        }

        public static void Register(IntPtr L)
        {
            // --- Input.GetKey(int) / Input.GetKeyDown(int) / Input.GetAxis(string) ---
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
            if (!ValidKeyCodes.Contains(keyCodeInt))
                return LuaNative.lua_error(L,
                    $"[InputBindings] GetKey: unknown KeyCode {keyCodeInt}");

            KeyCode code = (KeyCode)keyCodeInt;
            bool pressed = UnityEngine.Input.GetKey(code);
            LuaNative.lua_pushboolean(L, pressed ? 1 : 0);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetKeyDown(IntPtr L)
        {
            int keyCodeInt = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            if (!ValidKeyCodes.Contains(keyCodeInt))
                return LuaNative.lua_error(L,
                    $"[InputBindings] GetKeyDown: unknown KeyCode {keyCodeInt}");

            KeyCode code = (KeyCode)keyCodeInt;
            bool pressed = UnityEngine.Input.GetKeyDown(code);
            LuaNative.lua_pushboolean(L, pressed ? 1 : 0);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetAxis(IntPtr L)
        {
            int t = LuaNative.lua_type(L, 1);
            if (t != LuaNative.LUA_TSTRING && t != LuaNative.LUA_TNUMBER)
                return LuaNative.lua_error(L,
                    $"[InputBindings] GetAxis: expected string, got lua_type={t}");

            string axis = ReadString(L, 1);
            if (string.IsNullOrEmpty(axis))
                return LuaNative.lua_error(L,
                    "[InputBindings] GetAxis: empty axis name");

            string err = null;
            float v = 0f;
            try
            {
                v = UnityEngine.Input.GetAxis(axis);
            }
            catch (Exception e)
            {
                err = e.Message;
            }

            if (err != null)
                return LuaNative.lua_error(L,
                    $"[InputBindings] GetAxis('{axis}'): {err}");

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