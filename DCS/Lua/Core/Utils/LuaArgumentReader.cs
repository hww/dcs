using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DCS.Lua
{
    /// <summary>
    /// Helpers for reading Lua stack arguments in bindings.
    /// Three flavors per type:
    ///   Silent  — ReadX(L, idx)                     returns 0/null/false on miss
    ///   Strict  — ReadX(L, idx, fieldName)          raises luaL_error with stack dump
    ///   Default — ReadX(L, idx, fieldName, def)     returns def on miss
    ///   Try     — TryReadX(L, idx, out value)       returns bool
    /// </summary>
    public static class LuaArgumentReader
    {
        // ================= Int =================

        public static int ReadInt(IntPtr L, int idx)
        {
            return (int)LuaNative.lua_tointegerx(L, idx, IntPtr.Zero);
        }

        public static unsafe int ReadInt(IntPtr L, int idx, string fieldName)
        {
            int isNum;
            long val = LuaNative.lua_tointegerx(L, idx, (IntPtr)(&isNum));
            if (isNum == 0)
                LuaNative.luaL_error(L, BuildError(L, idx, "integer", fieldName));
            return (int)val;
        }

        public static unsafe int ReadInt(IntPtr L, int idx, string fieldName, int defaultValue)
        {
            int isNum;
            long val = LuaNative.lua_tointegerx(L, idx, (IntPtr)(&isNum));
            return isNum != 0 ? (int)val : defaultValue;
        }

        public static unsafe bool TryReadInt(IntPtr L, int idx, out int value)
        {
            int isNum;
            long val = LuaNative.lua_tointegerx(L, idx, (IntPtr)(&isNum));
            value = isNum != 0 ? (int)val : 0;
            return isNum != 0;
        }

        // ================= Float =================

        public static float ReadFloat(IntPtr L, int idx)
        {
            return (float)LuaNative.lua_tonumberx(L, idx, IntPtr.Zero);
        }

        public static unsafe float ReadFloat(IntPtr L, int idx, string fieldName)
        {
            int isNum;
            double val = LuaNative.lua_tonumberx(L, idx, (IntPtr)(&isNum));
            if (isNum == 0)
                LuaNative.luaL_error(L, BuildError(L, idx, "number", fieldName));
            return (float)val;
        }

        public static unsafe float ReadFloat(IntPtr L, int idx, string fieldName, float defaultValue)
        {
            int isNum;
            double val = LuaNative.lua_tonumberx(L, idx, (IntPtr)(&isNum));
            return isNum != 0 ? (float)val : defaultValue;
        }

        public static unsafe bool TryReadFloat(IntPtr L, int idx, out float value)
        {
            int isNum;
            double val = LuaNative.lua_tonumberx(L, idx, (IntPtr)(&isNum));
            value = isNum != 0 ? (float)val : 0f;
            return isNum != 0;
        }

        // ================= String =================

        public static string ReadString(IntPtr L, int idx)
        {
            IntPtr ptr = LuaNative.lua_tolstring(L, idx, IntPtr.Zero);
            return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : null;
        }

        public static string ReadString(IntPtr L, int idx, string fieldName)
        {
            IntPtr ptr = LuaNative.lua_tolstring(L, idx, IntPtr.Zero);
            if (ptr == IntPtr.Zero)
                LuaNative.luaL_error(L, BuildError(L, idx, "string", fieldName));
            return Marshal.PtrToStringUTF8(ptr);
        }

        public static string ReadString(IntPtr L, int idx, string fieldName, string defaultValue)
        {
            IntPtr ptr = LuaNative.lua_tolstring(L, idx, IntPtr.Zero);
            return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : defaultValue;
        }

        public static bool TryReadString(IntPtr L, int idx, out string value)
        {
            IntPtr ptr = LuaNative.lua_tolstring(L, idx, IntPtr.Zero);
            value = ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : null;
            return ptr != IntPtr.Zero;
        }

        // ================= Bool =================

        public static bool ReadBool(IntPtr L, int idx)
        {
            return LuaNative.lua_toboolean(L, idx) != 0;
        }

        public static bool ReadBool(IntPtr L, int idx, string fieldName)
        {
            int t = LuaNative.lua_type(L, idx);
            if (t != LuaNative.LUA_TBOOLEAN && t != LuaNative.LUA_TNIL)
                LuaNative.luaL_error(L, BuildError(L, idx, "boolean", fieldName));
            return LuaNative.lua_toboolean(L, idx) != 0;
        }

        public static bool ReadBool(IntPtr L, int idx, string fieldName, bool defaultValue)
        {
            int t = LuaNative.lua_type(L, idx);
            if (t != LuaNative.LUA_TBOOLEAN && t != LuaNative.LUA_TNIL)
                return defaultValue;
            return LuaNative.lua_toboolean(L, idx) != 0;
        }

        public static bool TryReadBool(IntPtr L, int idx, out bool value)
        {
            int t = LuaNative.lua_type(L, idx);
            if (t != LuaNative.LUA_TBOOLEAN && t != LuaNative.LUA_TNIL)
            {
                value = false;
                return false;
            }
            value = LuaNative.lua_toboolean(L, idx) != 0;
            return true;
        }

        // ================= Error formatting =================

        private static string BuildError(IntPtr L, int idx, string expected, string fieldName)
        {
            int type = LuaNative.lua_type(L, idx);
            string actual = TypeName(type);
            string dump = DumpStack(L);
            return $"[Lua Arg Error] field='{fieldName}' arg#{idx} expected {expected}, got {actual}\n{dump}";
        }

        private static string DumpStack(IntPtr L)
        {
            int top = LuaNative.lua_gettop(L);
            var sb = new StringBuilder();
            sb.Append("  stack (").Append(top).Append("):");
            if (top == 0)
                return sb.ToString();

            for (int i = 1; i <= top; i++)
            {
                sb.Append(' ');
                int t = LuaNative.lua_type(L, i);
                sb.Append('#').Append(i).Append('=').Append(TypeName(t));

                if (t == LuaNative.LUA_TNUMBER)
                {
                    double d = LuaNative.lua_tonumberx(L, i, IntPtr.Zero);
                    sb.Append('(').Append(d.ToString("G")).Append(')');
                }
                else if (t == LuaNative.LUA_TSTRING)
                {
                    IntPtr p = LuaNative.lua_tolstring(L, i, IntPtr.Zero);
                    string s = p != IntPtr.Zero ? Marshal.PtrToStringUTF8(p) : "";
                    if (s.Length > 20) s = s.Substring(0, 20) + "...";
                    sb.Append("(\"").Append(s).Append("\")");
                }
            }
            return sb.ToString();
        }

        private static string TypeName(int t)
        {
            switch (t)
            {
                case LuaNative.LUA_TNONE: return "none";
                case LuaNative.LUA_TNIL: return "nil";
                case LuaNative.LUA_TBOOLEAN: return "boolean";
                case LuaNative.LUA_TLIGHTUSERDATA: return "lightuserdata";
                case LuaNative.LUA_TNUMBER: return "number";
                case LuaNative.LUA_TSTRING: return "string";
                case LuaNative.LUA_TTABLE: return "table";
                case LuaNative.LUA_TFUNCTION: return "function";
                case LuaNative.LUA_TUSERDATA: return "userdata";
                case LuaNative.LUA_TTHREAD: return "thread";
                default: return $"<type {t}>";
            }
        }
    }
}