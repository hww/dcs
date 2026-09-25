using System;
using System.Runtime.InteropServices;
using AOT;
using DCS.Data;

namespace DCS.Lua.Bindings
{
    public static class VectorBindings
    {
        public static void Register(IntPtr L)
        {
            LuaBindings.RegisterGlobalFunction(L, Lua_AllocateVector, "Internal_AllocateVector");
            LuaBindings.RegisterGlobalFunction(L, Lua_RetainVector, "Internal_RetainVector");
            LuaBindings.RegisterGlobalFunction(L, Lua_ReleaseVector, "Internal_ReleaseVector");
            LuaBindings.RegisterGlobalFunction(L, Lua_VectorAdd, "Internal_VectorAdd");
            LuaBindings.RegisterGlobalFunction(L, Lua_VectorSub, "Internal_VectorSub");
            LuaBindings.RegisterGlobalFunction(L, Lua_GetVector, "Internal_GetVector");
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_AllocateVector(IntPtr L)
        {
            float x = (float)LuaNative.lua_tonumberx(L, 1, IntPtr.Zero);
            float y = (float)LuaNative.lua_tonumberx(L, 2, IntPtr.Zero);
            float z = (float)LuaNative.lua_tonumberx(L, 3, IntPtr.Zero);
            int idx = DCS_VectorAPI.AllocateVector(x, y, z);
            LuaNative.lua_pushinteger(L, idx);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_RetainVector(IntPtr L)
        {
            int idx = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            DCS_VectorAPI.RetainVector(idx);
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_ReleaseVector(IntPtr L)
        {
            int idx = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            DCS_VectorAPI.ReleaseVector(idx);
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_VectorAdd(IntPtr L)
        {
            int a = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int b = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            int r = (int)LuaNative.lua_tointegerx(L, 3, IntPtr.Zero);
            DCS_VectorAPI.VectorAdd(a, b, r);
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_VectorSub(IntPtr L)
        {
            int a = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int b = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            int r = (int)LuaNative.lua_tointegerx(L, 3, IntPtr.Zero);

            UnityEngine.Vector3 va = DCS_VectorAPI.VectorPool.Get(a);
            UnityEngine.Vector3 vb = DCS_VectorAPI.VectorPool.Get(b);
            DCS_VectorAPI.VectorPool.Set(r, va - vb);
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetVector(IntPtr L)
        {
            int idx = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            UnityEngine.Vector3 v = DCS_VectorAPI.VectorPool.Get(idx);
            LuaNative.lua_pushnumber(L, v.x);
            LuaNative.lua_pushnumber(L, v.y);
            LuaNative.lua_pushnumber(L, v.z);
            return 3;
        }
    }
}