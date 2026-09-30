using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace DCS.Lua
{
    public readonly ref struct ArgReader
    {
        private readonly IntPtr _L;
        private readonly int _top;
        private readonly string _funcName;

        public ArgReader(IntPtr L, string funcName)
        {
            Debug.Assert(L != IntPtr.Zero);
            _L = L;
            _top = LuaNative.lua_gettop(L);
            _funcName = funcName ?? "?";
        }

        public int Count => _top;
        public IntPtr LuaState => _L;

        // ============================================================
        // Арность
        // ============================================================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExpectExactly(int n)
        {
            if (_top != n)
                LuaFail.Fail(_L, _funcName,
                    $"expected exactly {n} argument{(n == 1 ? "" : "s")}, got {_top}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExpectAtLeast(int n)
        {
            if (_top < n)
                LuaFail.Fail(_L, _funcName,
                    $"expected at least {n} argument{(n == 1 ? "" : "s")}, got {_top}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExpectAtMost(int n)
        {
            if (_top > n)
                LuaFail.Fail(_L, _funcName,
                    $"expected at most {n} argument{(n == 1 ? "" : "s")}, got {_top}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExpectInRange(int min, int max)
        {
            if (_top < min || _top > max)
                LuaFail.Fail(_L, _funcName,
                    $"expected {min}..{max} arguments, got {_top}");
        }

        // ============================================================
        // Check* — строгие
        // ============================================================

        public string CheckString(int arg)
        {
            RequireArg(arg, "string");
            int t = LuaNative.lua_type(_L, arg);
            if (t != LuaNative.LUA_TSTRING && t != LuaNative.LUA_TNUMBER)
                FailArg(arg, $"expected string, got {TypeName(arg)}");
            return ReadStringRaw(arg);
        }

        public double CheckNumber(int arg)
        {
            RequireArg(arg, "number");
            if (LuaNative.lua_type(_L, arg) != LuaNative.LUA_TNUMBER)
                FailArg(arg, $"expected number, got {TypeName(arg)}");
            return LuaNative.lua_tonumberx(_L, arg, IntPtr.Zero);
        }

        public long CheckInteger(int arg)
        {
            RequireArg(arg, "integer");
            if (LuaNative.lua_type(_L, arg) != LuaNative.LUA_TNUMBER)
                FailArg(arg, $"expected integer, got {TypeName(arg)}");

            double d = LuaNative.lua_tonumberx(_L, arg, IntPtr.Zero);

            if (double.IsNaN(d))
                FailArg(arg, "expected integer, got NaN");
            if (d < long.MinValue || d > long.MaxValue)
                FailArg(arg, $"integer out of range: {d}");
            if (d != Math.Truncate(d))
                FailArg(arg, $"expected integer, got {d}");

            return (long)d;
        }

        public bool CheckBool(int arg)
        {
            RequireArg(arg, "boolean");
            if (LuaNative.lua_type(_L, arg) != LuaNative.LUA_TBOOLEAN)
                FailArg(arg, $"expected boolean, got {TypeName(arg)}");
            return LuaNative.lua_toboolean(_L, arg) != 0;
        }

        public IntPtr CheckUserdata(int arg, string metatableName)
        {
            RequireArg(arg, metatableName);
            IntPtr p = LuaNative.luaL_testudata(_L, arg, metatableName);
            if (p == IntPtr.Zero)
                FailArg(arg, $"expected {metatableName}, got {TypeName(arg)}");
            return p;
        }

        public IntPtr CheckUserdataRaw(int arg)
        {
            RequireArg(arg, "userdata");
            if (LuaNative.lua_type(_L, arg) != LuaNative.LUA_TUSERDATA)
                FailArg(arg, $"expected userdata, got {TypeName(arg)}");
            return LuaNative.lua_touserdata(_L, arg);
        }

        public void CheckTable(int arg)
        {
            RequireArg(arg, "table");
            if (LuaNative.lua_type(_L, arg) != LuaNative.LUA_TTABLE)
                FailArg(arg, $"expected table, got {TypeName(arg)}");
        }

        public void CheckFunction(int arg)
        {
            RequireArg(arg, "function");
            if (LuaNative.lua_type(_L, arg) != LuaNative.LUA_TFUNCTION)
                FailArg(arg, $"expected function, got {TypeName(arg)}");
        }

        public void CheckAny(int arg)
        {
            RequireArg(arg, "value");
        }

        // ============================================================
        // Try* — отличают "нет аргумента" от "nil"
        // ============================================================

        public bool TryString(int arg, out string value)
        {
            value = null;
            if (arg > _top || arg < 1) return false;
            int t = LuaNative.lua_type(_L, arg);
            if (t == LuaNative.LUA_TNONE) return false;
            if (t == LuaNative.LUA_TNIL) return true;
            if (t != LuaNative.LUA_TSTRING && t != LuaNative.LUA_TNUMBER)
                FailArg(arg, $"expected string or nil, got {TypeName(arg)}");
            value = ReadStringRaw(arg);
            return true;
        }

        public bool TryNumber(int arg, out double value)
        {
            value = 0;
            if (arg > _top || arg < 1) return false;
            int t = LuaNative.lua_type(_L, arg);
            if (t == LuaNative.LUA_TNONE) return false;
            if (t == LuaNative.LUA_TNIL) return true;
            if (t != LuaNative.LUA_TNUMBER)
                FailArg(arg, $"expected number or nil, got {TypeName(arg)}");
            value = LuaNative.lua_tonumberx(_L, arg, IntPtr.Zero);
            return true;
        }

        public bool TryInteger(int arg, out long value)
        {
            value = 0;
            if (arg > _top || arg < 1) return false;
            int t = LuaNative.lua_type(_L, arg);
            if (t == LuaNative.LUA_TNONE) return false;
            if (t == LuaNative.LUA_TNIL) return true;
            if (t != LuaNative.LUA_TNUMBER)
                FailArg(arg, $"expected integer or nil, got {TypeName(arg)}");
            double d = LuaNative.lua_tonumberx(_L, arg, IntPtr.Zero);
            if (d != Math.Truncate(d) || d < long.MinValue || d > long.MaxValue)
                FailArg(arg, $"expected integer or nil, got {d}");
            value = (long)d;
            return true;
        }

        public bool TryBool(int arg, out bool value)
        {
            value = false;
            if (arg > _top || arg < 1) return false;
            int t = LuaNative.lua_type(_L, arg);
            if (t == LuaNative.LUA_TNONE) return false;
            if (t == LuaNative.LUA_TNIL) return true;
            if (t != LuaNative.LUA_TBOOLEAN)
                FailArg(arg, $"expected boolean or nil, got {TypeName(arg)}");
            value = LuaNative.lua_toboolean(_L, arg) != 0;
            return true;
        }

        // ============================================================
        // Диагностика
        // ============================================================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RequireArg(int arg, string expected)
        {
            if (arg > _top || arg < 1)
                LuaFail.Fail(_L, _funcName,
                    $"missing argument #{arg} ({expected} expected), got {_top} argument{(_top == 1 ? "" : "s")}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void FailArg(int arg, string reason)
        {
            LuaFail.Fail(_L, _funcName, $"argument #{arg}: {reason}");
        }

        public string TypeName(int arg)
        {
            int t = LuaNative.lua_type(_L, arg);
            switch (t)
            {
                case LuaNative.LUA_TNONE: return "no value";
                case LuaNative.LUA_TNIL: return "nil";
                case LuaNative.LUA_TBOOLEAN: return "boolean";
                case LuaNative.LUA_TLIGHTUSERDATA: return "light userdata";
                case LuaNative.LUA_TNUMBER: return "number";
                case LuaNative.LUA_TSTRING: return "string";
                case LuaNative.LUA_TTABLE: return "table";
                case LuaNative.LUA_TFUNCTION: return "function";
                case LuaNative.LUA_TUSERDATA: return "userdata";
                case LuaNative.LUA_TTHREAD: return "thread";
                default: return $"<type {t}>";
            }
        }

        private string ReadStringRaw(int arg)
        {
            // Используем overload с out UIntPtr (второй в LuaNative)
            IntPtr p = LuaNative.lua_tolstring(_L, arg, out UIntPtr len);
            if (p == IntPtr.Zero) return null;
            int n = checked((int)len);
            if (n == 0) return string.Empty;

            // Fallback, работает везде — .NET Framework, .NET Standard 2.0, .NET 5+
            byte[] buf = new byte[n];
            Marshal.Copy(p, buf, 0, n);
            return Encoding.UTF8.GetString(buf);
        }
    }
}