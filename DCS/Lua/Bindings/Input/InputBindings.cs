using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace DCS.Lua
{
    public static class InputBindings
    {
        public static void Register(IntPtr L)
        {
            LuaBindings.RegisterNamespace(L, "Input", (state, tableIndex) =>
            {
                LuaBindings.RegisterMethod(state, Lua_GetKey, tableIndex, "GetKey");
                LuaBindings.RegisterMethod(state, Lua_GetKeyDown, tableIndex, "GetKeyDown");
                LuaBindings.RegisterMethod(state, Lua_GetAxis, tableIndex, "GetAxis");
            });

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
            var args = new ArgReader(L, "Input.GetKey");
            args.ExpectExactly(1);

            KeyCode code = (KeyCode)(int)args.CheckInteger(1);
            LuaNative.lua_pushboolean(L, UnityEngine.Input.GetKey(code) ? 1 : 0);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetKeyDown(IntPtr L)
        {
            var args = new ArgReader(L, "Input.GetKeyDown");
            args.ExpectExactly(1);

            KeyCode code = (KeyCode)(int)args.CheckInteger(1);
            LuaNative.lua_pushboolean(L, UnityEngine.Input.GetKeyDown(code) ? 1 : 0);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetAxis(IntPtr L)
        {
            var args = new ArgReader(L, "Input.GetAxis");
            args.ExpectExactly(1);

            string axis = args.CheckString(1);
            if (string.IsNullOrEmpty(axis))
                LuaFail.Fail(L, "Input.GetAxis", "argument #1: axis name must not be empty");

            float v = UnityEngine.Input.GetAxis(axis);
            LuaNative.lua_pushnumber(L, v);
            return 1;
        }
    }
}