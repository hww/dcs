using System;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;

namespace DCS.Lua.Bindings
{
    public static class SceneBindings
    {
        public static void Register(IntPtr L)
        {
            LuaBindings.RegisterNamespace(L, "Scene", (state, tableIndex) =>
            {
                LuaBindings.RegisterMethod(state, Lua_Load, tableIndex, "Load");
                LuaBindings.RegisterMethod(state, Lua_Unload, tableIndex, "Unload");
                LuaBindings.RegisterMethod(state, Lua_GetCurrent, tableIndex, "GetCurrent");
            });
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Load(IntPtr L)
        {
            string name = LuaArgumentReader.ReadString(L, 1);
            if (string.IsNullOrEmpty(name))
                return 0;

            SceneManager.LoadScene(name);
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Unload(IntPtr L)
        {
            string name = LuaArgumentReader.ReadString(L, 1);
            if (string.IsNullOrEmpty(name))
                return 0;

            SceneManager.UnloadSceneAsync(name);
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetCurrent(IntPtr L)
        {
            LuaNative.lua_pushstring(L, SceneManager.GetActiveScene().name);
            return 1;
        }
    }
}