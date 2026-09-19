using System;
using System.Runtime.InteropServices;
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
                // Забираем первый аргумент, строго как в твоем исходнике
                IntPtr ptr = LuaNative.lua_tolstring(L, 1, IntPtr.Zero);

                // Железная защита от NULL указателей
                if (ptr == IntPtr.Zero)
                {
                    if (ActiveLogRedirect != null) ActiveLogRedirect("nil");
                    else Debug.Log("[Lua] nil");
                    return 0;
                }

                string message = Marshal.PtrToStringAnsi(ptr);

                if (ActiveLogRedirect != null)
                {
                    ActiveLogRedirect(message);
                }
                else
                {
                    Debug.Log($"[Lua] {message}");
                }
            }
            catch (Exception ex)
            {
                // Если что-то пошло не так, просто пишем в Unity, не давая упасть нативной части
                Debug.LogError($"[Lua Print Guard] {ex.Message}");
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
            if (LuaNative.luaL_loadstring(L, code) == 0)
            {
                if (LuaNative.lua_pcallk(L, 0, -1, 0, 0, IntPtr.Zero) != 0)
                {
                    string error = GetStringFromStack(-1);
                    Debug.LogError($"[Lua Runtime Error In {_stateName}] {error}");
                }
            }
            else
            {
                string error = GetStringFromStack(-1);
                Debug.LogError($"[Lua Syntax Error In {_stateName}] {error}");
            }
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
