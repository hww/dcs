using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DynamicComponent.Lua
{
    public class LuaStateWrapper : IDisposable
    {
        public IntPtr L { get; private set; }
        private readonly string _stateName;

        public LuaStateWrapper(string stateName = "Global")
        {
            _stateName = stateName;
            L = LuaNative.luaL_newstate();
            if (L == IntPtr.Zero) throw new InvalidOperationException("Failed to create Lua state.");

            LuaNative.luaL_openlibs(L);

            // КОНКРЕТНОЕ ИСПРАВЛЕНИЕ: Переопределяем стандартный Lua-макрос print на наш C#-метод
            IntPtr printPtr = Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_Print);
            LuaNative.lua_pushcclosure(L, printPtr, 0);
            LuaNative.lua_setglobal(L, "print");
        }

        // Чистый PInvoke-коллбэк, который перехватывает вызовы print() из Lua
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Print(IntPtr L)
        {
            // Извлекаем строку из стека Lua (первый аргумент)
            IntPtr ptr = LuaNative.lua_tolstring(L, 1, IntPtr.Zero);
            string message = Marshal.PtrToStringAnsi(ptr);

            // Выводим в родную консоль Unity
            Debug.Log($"[Lua] {message}");
            return 0;
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

        public string GetStringFromStack(int index)
        {
            IntPtr ptr = LuaNative.lua_tolstring(L, index, IntPtr.Zero);
            return Marshal.PtrToStringAnsi(ptr);
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
