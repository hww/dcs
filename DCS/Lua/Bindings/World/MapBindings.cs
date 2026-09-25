using DCS.Core;
using DCS.Spatial;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DCS.Lua.Bindings
{
    public static class MapBindings
    {
        public static void Register(IntPtr L)
        {
            LuaBindings.RegisterNamespace(L, "Map", (state, tableIndex) =>
            {
                LuaBindings.RegisterMethod(state, Lua_StreamMap, tableIndex, "Stream");
                LuaBindings.RegisterMethod(state, Lua_UnloadMap, tableIndex, "Unload");
                LuaBindings.RegisterMethod(state, Lua_GetMapStatus, tableIndex, "GetStatus");
            });
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_StreamMap(IntPtr L)
        {
            string mapName = LuaArgumentReader.ReadString(L, 1);
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
    }
}