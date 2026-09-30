using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DCS.Lua
{
    /// <summary>
    /// Userdata-представление Vector3 в Lua.
    /// Layout userdata: 3 float (x, y, z) — 12 байт.
    /// Метатаблица "Vector3" регистрируется один раз в RegisterMetatable.
    /// </summary>
    public static class VectorMarshalling
    {
        public const string MetatableName = "Vector3";
        public const int SizeInBytes = sizeof(float) * 3;

        // ---- Push ----

        public static void PushVector3(IntPtr L, Vector3 v)
        {
            IntPtr ud = LuaNative.lua_newuserdatauv(L, (UIntPtr)SizeInBytes, 0);
            WriteRaw(ud, v);
            LuaNative.luaL_setmetatable(L, MetatableName);
        }

        // ---- Read ----

        /// <summary>
        /// Пытается прочитать Vector3 с индекса idx.
        /// Принимает:
        ///   - userdata с метатаблицей "Vector3"
        ///   - (опционально) 3 числа подряд — начиная с idx
        /// </summary>
        public static bool TryReadVector3(IntPtr L, int idx, out Vector3 v)
        {
            v = default;

            int t = LuaNative.lua_type(L, idx);
            if (t == LuaNative.LUA_TUSERDATA)
            {
                IntPtr ud = LuaNative.lua_touserdata(L, idx);
                if (ud == IntPtr.Zero) return false;
                // Проверка метатаблицы
                if (LuaNative.luaL_testudata(L, idx, MetatableName) == IntPtr.Zero)
                    return false;
                v = ReadRaw(ud);
                return true;
            }

            if (t == LuaNative.LUA_TNUMBER)
            {
                // Три числа: idx, idx+1, idx+2
                unsafe
                {
                    int isX, isY, isZ;
                    double x = LuaNative.lua_tonumberx(L, idx, (IntPtr)(&isX));
                    double y = LuaNative.lua_tonumberx(L, idx + 1, (IntPtr)(&isY));
                    double z = LuaNative.lua_tonumberx(L, idx + 2, (IntPtr)(&isZ));
                    if (isX != 0 && isY != 0 && isZ != 0)
                    {
                        v = new Vector3((float)x, (float)y, (float)z);
                        return true;
                    }
                }
            }

            return false;
        }

        // ---- Raw access (unsafe) ----

        public static unsafe void WriteRaw(IntPtr ud, Vector3 v)
        {
            float* p = (float*)ud.ToPointer();
            p[0] = v.x;
            p[1] = v.y;
            p[2] = v.z;
        }

        public static unsafe Vector3 ReadRaw(IntPtr ud)
        {
            float* p = (float*)ud.ToPointer();
            return new Vector3(p[0], p[1], p[2]);
        }

        public static unsafe float* GetPtr(IntPtr ud)
        {
            return (float*)ud.ToPointer();
        }

        public static Vector3 ReadVector3OrDefault(IntPtr L, int idx)
        {
            TryReadVector3(L, idx, out var v);
            return v;
        }
    }
}