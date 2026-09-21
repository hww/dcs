using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace DCS.Lua
{
    public class LuaRepl
    {
        private readonly IntPtr _L;
        private const int LUA_OK = 0;

        public LuaRepl(IntPtr L)
        {
            if (L == IntPtr.Zero) throw new ArgumentException("lua_State is null");
            _L = L;
        }

        public string Eval(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            int startTop = LuaNative.lua_gettop(_L);
            var stdoutAccumulator = new StringBuilder();
            LuaStateWrapper.ActiveLogRedirect = (msg) => stdoutAccumulator.AppendLine(msg);

            try
            {
                int topBeforeRun = LuaNative.lua_gettop(_L);

                // Кладём debug.traceback под chunk
                LuaNative.lua_getglobal(_L, "debug");
                LuaNative.lua_getfield(_L, -1, "traceback");
                LuaNative.lua_rotate(_L, -2, -1);
                LuaNative.lua_settop(_L, -2);
                // Пробуем как выражение
                int status = LuaNative.luaL_loadstring(_L, "return " + input);
                if (status != LUA_OK)
                {
                    LuaNative.lua_settop(_L, topBeforeRun + 1);
                    status = LuaNative.luaL_loadstring(_L, input);
                }

                if (status != LUA_OK)
                {
                    IntPtr ptr = LuaNative.lua_tolstring(_L, -1, IntPtr.Zero);
                    string err = (ptr != IntPtr.Zero) ? Marshal.PtrToStringAnsi(ptr) : "unknown syntax error";
                    return $"\u001b[91mSYNTAX ERROR: {err}\u001b[0m\n";
                }

                // Стек: [..., traceback, chunk]
                status = LuaNative.lua_pcallk(_L, 0, -1, -2, 0, IntPtr.Zero);
                if (status != LUA_OK)
                {
                    IntPtr ptr = LuaNative.lua_tolstring(_L, -1, IntPtr.Zero);
                    string err = (ptr != IntPtr.Zero) ? Marshal.PtrToStringAnsi(ptr) : "unknown runtime error";
                    return $"\u001b[91mRUNTIME ERROR: {err}\u001b[0m\n";
                }

                // Стек: [..., traceback, result1, result2, ...]
                int currentTop = LuaNative.lua_gettop(_L);
                int nres = currentTop - topBeforeRun - 1;

                var resultBuilder = new StringBuilder();
                if (stdoutAccumulator.Length > 0)
                    resultBuilder.Append(stdoutAccumulator.ToString());

                for (int i = 1; i <= nres; i++)
                {
                    if (i > 1) resultBuilder.Append('\t');
                    resultBuilder.Append(FormatValueSafe(_L, topBeforeRun + 1 + i));
                }
                if (nres > 0) resultBuilder.Append('\n');
                if (resultBuilder.Length == 0) resultBuilder.Append("ok\n");
                return resultBuilder.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return $"INTERNAL REPL EXCEPTION: {ex}\n";
            }
            finally
            {
                LuaStateWrapper.ActiveLogRedirect = null;
                LuaNative.lua_settop(_L, startTop);
            }
        }

        private string FormatValueSafe(IntPtr L, int idx)
        {
            int t = LuaNative.lua_type(L, idx);
            switch (t)
            {
                case LuaNative.LUA_TNIL: return "nil";
                case LuaNative.LUA_TBOOLEAN: return LuaNative.lua_toboolean(L, idx) != 0 ? "true" : "false";
                case LuaNative.LUA_TNUMBER:
                    {
                        double d = LuaNative.lua_tonumberx(L, idx, IntPtr.Zero);
                        long i = LuaNative.lua_tointegerx(L, idx, IntPtr.Zero);
                        return (d == i) ? i.ToString() : d.ToString("G");
                    }
                case LuaNative.LUA_TSTRING:
                    {
                        IntPtr ptr = LuaNative.lua_tolstring(L, idx, IntPtr.Zero);
                        return (ptr != IntPtr.Zero) ? Marshal.PtrToStringAnsi(ptr) : "";
                    }
                case LuaNative.LUA_TTABLE: return "table";
                case LuaNative.LUA_TFUNCTION: return "function";
                case LuaNative.LUA_TUSERDATA: return "userdata";
                default: return $"<type {t}>";
            }
        }

        public bool IsCodeComplete(string input, out bool syntaxError)
        {
            syntaxError = false;
            if (string.IsNullOrWhiteSpace(input)) return true;

            int startTop = LuaNative.lua_gettop(_L);
            try
            {
                int status = LuaNative.luaL_loadstring(_L, "return " + input);
                if (status == 0) return true;

                LuaNative.lua_settop(_L, startTop);
                status = LuaNative.luaL_loadstring(_L, input);
                if (status == 0) return true;

                IntPtr ptr = LuaNative.lua_tolstring(_L, -1, IntPtr.Zero);
                string err = (ptr != IntPtr.Zero) ? Marshal.PtrToStringAnsi(ptr) : "";

                if (err.Contains("<eof>"))
                    return false;

                syntaxError = true;
                return true;
            }
            finally
            {
                LuaNative.lua_settop(_L, startTop);
            }
        }
    }
}