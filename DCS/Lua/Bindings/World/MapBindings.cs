using System;
using System.Runtime.InteropServices;
using DCS.Core;
using DCS.Spatial;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DCS.Lua.Bindings
{
    /// <summary>World/map streaming API. Existing Map function names are preserved.</summary>
    public static class MapBindings
    {
        public static void Register(IntPtr L)
        {
            LuaNative.lua_newtable(L);
            LuaBindings.RegisterMethod(L, Lua_StreamMap, "Stream");
            LuaBindings.RegisterMethod(L, Lua_UnloadMap, "Unload");
            LuaBindings.RegisterMethod(L, Lua_GetMapStatus, "GetStatus");
            LuaNative.lua_setglobal(L, "Map");
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_StreamMap(IntPtr L)
        {
            string mapName = ReadString(L, 1);
            if (string.IsNullOrEmpty(mapName))
                return 0;

            MonoBehaviour runner = SpatialRuntime.Instance;
            if (runner != null)
                DatasetLoader.StreamMapAsync(mapName, runner);

            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_UnloadMap(IntPtr L)
        {
            string currentMap = DatasetLoader.CurrentMapName;
            if (!string.IsNullOrEmpty(currentMap))
            {
                DatasetLoader.UnloadMapDataset();
                SceneManager.UnloadSceneAsync(currentMap);
            }

            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetMapStatus(IntPtr L)
        {
            string state = DatasetLoader.IsLoading
                ? "Loading"
                : (!string.IsNullOrEmpty(DatasetLoader.CurrentMapName) ? "Loaded" : "Unloaded");

            LuaNative.lua_pushstring(L, state);
            LuaNative.lua_pushstring(L, DatasetLoader.CurrentMapName ?? string.Empty);
            LuaNative.lua_pushnumber(L, Math.Round(DatasetLoader.LoadingProgress * 100.0, 1));
            return 3;
        }

        private static string ReadString(IntPtr L, int index)
        {
            IntPtr ptr = LuaNative.lua_tolstring(L, index, IntPtr.Zero);
            return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : null;
        }
    }
}
