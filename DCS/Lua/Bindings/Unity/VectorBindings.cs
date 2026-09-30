using System;
using System.Runtime.InteropServices;
using AOT;
using DCS.Data;

namespace DCS.Lua
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

        // ============================================================
        //  AllocateVector(x, y, z) -> idx
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_AllocateVector(IntPtr L)
        {
            var args = new ArgReader(L, "AllocateVector");
            args.ExpectExactly(3);

            float x = (float)args.CheckNumber(1);
            float y = (float)args.CheckNumber(2);
            float z = (float)args.CheckNumber(3);

            int idx = DCS_VectorAPI.AllocateVector(x, y, z);
            LuaNative.lua_pushinteger(L, idx);
            return 1;
        }

        // ============================================================
        //  RetainVector(idx)
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_RetainVector(IntPtr L)
        {
            var args = new ArgReader(L, "RetainVector");
            args.ExpectExactly(1);

            int idx = (int)args.CheckInteger(1);
            DCS_VectorAPI.RetainVector(idx);
            return 0;
        }

        // ============================================================
        //  ReleaseVector(idx)
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_ReleaseVector(IntPtr L)
        {
            var args = new ArgReader(L, "ReleaseVector");
            args.ExpectExactly(1);

            int idx = (int)args.CheckInteger(1);
            DCS_VectorAPI.ReleaseVector(idx);
            return 0;
        }

        // ============================================================
        //  VectorAdd(a, b, r)
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_VectorAdd(IntPtr L)
        {
            var args = new ArgReader(L, "VectorAdd");
            args.ExpectExactly(3);

            int a = (int)args.CheckInteger(1);
            int b = (int)args.CheckInteger(2);
            int r = (int)args.CheckInteger(3);

            DCS_VectorAPI.VectorAdd(a, b, r);
            return 0;
        }

        // ============================================================
        //  VectorSub(a, b, r)
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_VectorSub(IntPtr L)
        {
            var args = new ArgReader(L, "VectorSub");
            args.ExpectExactly(3);

            int a = (int)args.CheckInteger(1);
            int b = (int)args.CheckInteger(2);
            int r = (int)args.CheckInteger(3);

            UnityEngine.Vector3 va = DCS_VectorAPI.VectorPool.Get(a);
            UnityEngine.Vector3 vb = DCS_VectorAPI.VectorPool.Get(b);
            DCS_VectorAPI.VectorPool.Set(r, va - vb);
            return 0;
        }

        // ============================================================
        //  GetVector(idx) -> x, y, z
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetVector(IntPtr L)
        {
            var args = new ArgReader(L, "GetVector");
            args.ExpectExactly(1);

            int idx = (int)args.CheckInteger(1);

            UnityEngine.Vector3 v = DCS_VectorAPI.VectorPool.Get(idx);
            LuaNative.lua_pushnumber(L, v.x);
            LuaNative.lua_pushnumber(L, v.y);
            LuaNative.lua_pushnumber(L, v.z);
            return 3;
        }
    }
}