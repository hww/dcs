using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace DCS.Lua
{
    public class LuaStateWrapper : IDisposable
    {
        public IntPtr L { get; private set; }
        private readonly string _stateName;

        // Используем встроенный в .NET делегат Func<IntPtr, int> — именно он работал у тебя изначально!
        private readonly Func<IntPtr, int> _printDelegate;

        public static Action<string> ActiveLogRedirect;

        public LuaStateWrapper(string stateName = "Global")
        {
            _stateName = stateName;
            L = LuaNative.luaL_newstate();
            if (L == IntPtr.Zero) throw new InvalidOperationException("Failed to create Lua state.");

            LuaNative.luaL_openlibs(L);

            // КРИСТАЛЬНО ТОЧНОЕ ВОССТАНОВЛЕНИЕ ТВОЕГО СТАРОГО КОДА:
            _printDelegate = Lua_Print;
            IntPtr printPtr = Marshal.GetFunctionPointerForDelegate(_printDelegate);
            LuaNative.lua_pushcclosure(L, printPtr, 0);
            LuaNative.lua_setglobal(L, "print");
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Print(IntPtr L)
        {
            try
            {
                int n = LuaNative.lua_gettop(L);
                var sb = new StringBuilder();

                for (int i = 1; i <= n; i++)
                {
                    if (i > 1) sb.Append('\t');

                    int t = LuaNative.lua_type(L, i);
                    switch (t)
                    {
                        case LuaNative.LUA_TNIL: sb.Append("nil"); break;
                        case LuaNative.LUA_TBOOLEAN: sb.Append(LuaNative.lua_toboolean(L, i) != 0 ? "true" : "false"); break;
                        case LuaNative.LUA_TNUMBER:
                            {
                                double d = LuaNative.lua_tonumberx(L, i, IntPtr.Zero);
                                long l = LuaNative.lua_tointegerx(L, i, IntPtr.Zero);
                                sb.Append(d == l ? l.ToString() : d.ToString("G"));
                                break;
                            }
                        case LuaNative.LUA_TSTRING:
                            {
                                UIntPtr len;
                                IntPtr p = LuaNative.lua_tolstring(L, i, out len);
                                if (p != IntPtr.Zero && (int)len > 0)
                                {
                                    byte[] buf = new byte[(int)len];
                                    Marshal.Copy(p, buf, 0, (int)len);
                                    sb.Append(Encoding.UTF8.GetString(buf));
                                }
                                break;
                            }
                        default:
                            sb.Append($"<type {t}>");
                            break;
                    }
                }

                string message = sb.ToString();
                if (ActiveLogRedirect != null) 
                    ActiveLogRedirect(message);
                Debug.Log($"[Lua] {message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Lua Print Guard] {ex}");
            }
            return 0;
        }

        public string GetStringFromStack(int index)
        {
            IntPtr ptr = LuaNative.lua_tolstring(L, index, IntPtr.Zero);
            if (ptr == IntPtr.Zero) return "";
            return Marshal.PtrToStringAnsi(ptr);
        }

        public void ExecuteString(string code, string chunkName = "chunk")
        {
            int startTop = LuaNative.lua_gettop(L);

            // Кладём traceback
            LuaNative.lua_getglobal(L, "debug");
            LuaNative.lua_getfield(L, -1, "traceback");
            LuaNative.lua_rotate(L, -2, -1);   // <-- вместо lua_remove
                                               // Стек: [..., traceback]
            LuaNative.lua_settop(L, -2);
            // Кладём chunk
            if (LuaNative.luaL_loadstring(L, code) != 0)
            {
                string error = GetStringFromStack(-1);
                Debug.LogError($"[Lua Syntax Error In {_stateName}] {error}");
                LuaNative.lua_settop(L, startTop);
                return;
            }
            // Стек: [..., traceback, chunk]

            // Вызов. Функция = chunk (top). msgh = traceback (-2).
            if (LuaNative.lua_pcallk(L, 0, -1, -2, 0, IntPtr.Zero) != 0)
            {
                string error = GetStringFromStack(-1);
                Debug.LogError($"[Lua Runtime Error In {_stateName}] {error}");
            }

            LuaNative.lua_settop(L, startTop);
        }

        public void Dispose()
        {
            if (L != IntPtr.Zero)
            {
                LuaNative.lua_close(L);
                L = IntPtr.Zero;
            }
        }
    }
}
