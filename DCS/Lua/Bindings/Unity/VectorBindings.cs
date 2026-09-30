using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace DCS.Lua
{
    public static class VectorBindings
    {
        public static void Register(IntPtr L)
        {
            RegisterMetatable(L);

            // Глобальный конструктор: Vector3(x, y, z)
            LuaNative.lua_pushcclosure(L,
                Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_Vector3_Ctor), 0);
            LuaNative.lua_setglobal(L, "Vector3");
        }

        // ============================================================
        //  Метатаблица "Vector3"
        // ============================================================

        private static void RegisterMetatable(IntPtr L)
        {
            if (LuaNative.luaL_newmetatable(L, VectorMarshalling.MetatableName) == 0)
            {
                // Уже зарегистрирована.
                LuaNative.lua_settop(L, -2);
                return;
            }

            // __index
            LuaNative.lua_pushstring(L, "__index");
            LuaNative.lua_pushcclosure(L,
                Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_Vector3_Index), 0);
            LuaNative.lua_settable(L, -3);

            // __newindex
            LuaNative.lua_pushstring(L, "__newindex");
            LuaNative.lua_pushcclosure(L,
                Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_Vector3_NewIndex), 0);
            LuaNative.lua_settable(L, -3);

            // __add
            LuaNative.lua_pushstring(L, "__add");
            LuaNative.lua_pushcclosure(L,
                Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_Vector3_Add), 0);
            LuaNative.lua_settable(L, -3);

            // __sub
            LuaNative.lua_pushstring(L, "__sub");
            LuaNative.lua_pushcclosure(L,
                Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_Vector3_Sub), 0);
            LuaNative.lua_settable(L, -3);

            // __mul
            LuaNative.lua_pushstring(L, "__mul");
            LuaNative.lua_pushcclosure(L,
                Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_Vector3_Mul), 0);
            LuaNative.lua_settable(L, -3);

            // __unm
            LuaNative.lua_pushstring(L, "__unm");
            LuaNative.lua_pushcclosure(L,
                Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_Vector3_Unm), 0);
            LuaNative.lua_settable(L, -3);

            // __eq
            LuaNative.lua_pushstring(L, "__eq");
            LuaNative.lua_pushcclosure(L,
                Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_Vector3_Eq), 0);
            LuaNative.lua_settable(L, -3);

            // __tostring
            LuaNative.lua_pushstring(L, "__tostring");
            LuaNative.lua_pushcclosure(L,
                Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_Vector3_ToString), 0);
            LuaNative.lua_settable(L, -3);

            // __name (для print)
            LuaNative.lua_pushstring(L, "__name");
            LuaNative.lua_pushstring(L, "Vector3");
            LuaNative.lua_settable(L, -3);

            LuaNative.lua_settop(L, -2);
        }

        // ============================================================
        //  Vector3(x, y, z)  /  Vector3(v)  /  Vector3()
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Vector3_Ctor(IntPtr L)
        {
            int n = LuaNative.lua_gettop(L);

            if (n == 0)
            {
                VectorMarshalling.PushVector3(L, Vector3.zero);
                return 1;
            }

            if (n == 1)
            {
                if (VectorMarshalling.TryReadVector3(L, 1, out Vector3 v))
                {
                    VectorMarshalling.PushVector3(L, v);
                    return 1;
                }
                LuaFail.Fail(L, "Vector3", "single argument must be a Vector3");
                return 0;
            }

            if (n == 3)
            {
                var args = new ArgReader(L, "Vector3");
                float x = (float)args.CheckNumber(1);
                float y = (float)args.CheckNumber(2);
                float z = (float)args.CheckNumber(3);
                VectorMarshalling.PushVector3(L, new Vector3(x, y, z));
                return 1;
            }

            LuaFail.Fail(L, "Vector3",
                $"expected 0, 1, or 3 arguments, got {n}");
            return 0;
        }

        // ============================================================
        //  __index: v.x / v.y / v.z
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Vector3_Index(IntPtr L)
        {
            IntPtr ud = LuaNative.luaL_testudata(L, 1, VectorMarshalling.MetatableName);
            if (ud == IntPtr.Zero)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            string key = LuaArgumentReader.ReadString(L, 2);
            unsafe
            {
                float* p = VectorMarshalling.GetPtr(ud);
                switch (key)
                {
                    case "x": LuaNative.lua_pushnumber(L, p[0]); return 1;
                    case "y": LuaNative.lua_pushnumber(L, p[1]); return 1;
                    case "z": LuaNative.lua_pushnumber(L, p[2]); return 1;
                }
            }
            LuaNative.lua_pushnil(L);
            return 1;
        }

        // ============================================================
        //  __newindex: v.x = 5
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Vector3_NewIndex(IntPtr L)
        {
            IntPtr ud = LuaNative.luaL_testudata(L, 1, VectorMarshalling.MetatableName);
            if (ud == IntPtr.Zero)
            {
                LuaFail.Fail(L, "Vector3.__newindex", "invalid Vector3 userdata");
                return 0;
            }

            string key = LuaArgumentReader.ReadString(L, 2);
            float value = (float)LuaNative.lua_tonumberx(L, 3, IntPtr.Zero);

            unsafe
            {
                float* p = VectorMarshalling.GetPtr(ud);
                switch (key)
                {
                    case "x": p[0] = value; return 0;
                    case "y": p[1] = value; return 0;
                    case "z": p[2] = value; return 0;
                }
            }
            LuaFail.Fail(L, "Vector3.__newindex",
                $"unknown field '{key}' (expected x, y, z)");
            return 0;
        }

        // ============================================================
        //  __add: v1 + v2
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Vector3_Add(IntPtr L)
        {
            if (!VectorMarshalling.TryReadVector3(L, 1, out Vector3 a) ||
                !VectorMarshalling.TryReadVector3(L, 2, out Vector3 b))
            {
                LuaFail.Fail(L, "Vector3.__add", "both operands must be Vector3");
                return 0;
            }
            VectorMarshalling.PushVector3(L, a + b);
            return 1;
        }

        // ============================================================
        //  __sub: v1 - v2
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Vector3_Sub(IntPtr L)
        {
            if (!VectorMarshalling.TryReadVector3(L, 1, out Vector3 a) ||
                !VectorMarshalling.TryReadVector3(L, 2, out Vector3 b))
            {
                LuaFail.Fail(L, "Vector3.__sub", "both operands must be Vector3");
                return 0;
            }
            VectorMarshalling.PushVector3(L, a - b);
            return 1;
        }

        // ============================================================
        //  __mul: v * scalar  /  scalar * v
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Vector3_Mul(IntPtr L)
        {
            bool aIsVec = VectorMarshalling.TryReadVector3(L, 1, out Vector3 a);
            bool bIsVec = VectorMarshalling.TryReadVector3(L, 2, out Vector3 b);

            if (aIsVec && bIsVec)
            {
                // v1 * v2 — покомпонентно (как в шейдерах)
                VectorMarshalling.PushVector3(L,
                    new Vector3(a.x * b.x, a.y * b.y, a.z * b.z));
                return 1;
            }
            if (aIsVec)
            {
                float s = (float)LuaNative.lua_tonumberx(L, 2, IntPtr.Zero);
                VectorMarshalling.PushVector3(L, a * s);
                return 1;
            }
            if (bIsVec)
            {
                float s = (float)LuaNative.lua_tonumberx(L, 1, IntPtr.Zero);
                VectorMarshalling.PushVector3(L, b * s);
                return 1;
            }

            LuaFail.Fail(L, "Vector3.__mul", "expected Vector3 * number or number * Vector3");
            return 0;
        }

        // ============================================================
        //  __unm: -v
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Vector3_Unm(IntPtr L)
        {
            if (!VectorMarshalling.TryReadVector3(L, 1, out Vector3 v))
            {
                LuaFail.Fail(L, "Vector3.__unm", "operand must be Vector3");
                return 0;
            }
            VectorMarshalling.PushVector3(L, -v);
            return 1;
        }

        // ============================================================
        //  __eq: v1 == v2
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Vector3_Eq(IntPtr L)
        {
            if (!VectorMarshalling.TryReadVector3(L, 1, out Vector3 a) ||
                !VectorMarshalling.TryReadVector3(L, 2, out Vector3 b))
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }
            LuaNative.lua_pushboolean(L, a == b ? 1 : 0);
            return 1;
        }

        // ============================================================
        //  __tostring: tostring(v)
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Vector3_ToString(IntPtr L)
        {
            if (!VectorMarshalling.TryReadVector3(L, 1, out Vector3 v))
            {
                LuaNative.lua_pushstring(L, "<invalid Vector3>");
                return 1;
            }
            string s = $"({v.x:0.###}, {v.y:0.###}, {v.z:0.###})";
            LuaNative.lua_pushstring(L, s);
            return 1;
        }
    }
}