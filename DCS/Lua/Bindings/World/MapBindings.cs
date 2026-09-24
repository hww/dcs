using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using DCS.Spatial;
using DCS.Core;

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

        // ------------------------------------------------------------
        //  Map.Stream(mapName) -> bool
        //  true  — загрузка запущена
        //  false — уже идёт другая загрузка
        //  Команда: пустое имя / нет runner'а — ошибка.
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_StreamMap(IntPtr L)
        {
            string mapName = ReadString(L, 1);
            if (string.IsNullOrEmpty(mapName))
                return LuaNative.lua_error(L,
                    "[MapBindings] Stream: map name is empty");

            MonoBehaviour runner = SpatialRuntime.Instance;
            if (runner == null)
                return LuaNative.lua_error(L,
                    "[MapBindings] Stream: SpatialRuntime instance not found");

            if (DatasetLoader.IsLoading)
            {
                // Уже грузим что-то другое — не ошибка, просто отказ.
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            DatasetLoader.StreamMapAsync(mapName, runner);
            LuaNative.lua_pushboolean(L, 1);
            return 1;
        }

        // ------------------------------------------------------------
        //  Map.Unload() -> void
        //  Если карты нет — тихий no-op (это валидный сценарий).
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_UnloadMap(IntPtr L)
        {
            string currentMap = DatasetLoader.CurrentMapName;
            if (string.IsNullOrEmpty(currentMap))
                return 0;

            DatasetLoader.UnloadMapDataset();
            var op = SceneManager.UnloadSceneAsync(currentMap);
            if (op == null)
                Debug.LogWarning(
                    $"[MapBindings] Unload: scene '{currentMap}' is not loaded additively");

            return 0;
        }

        // ------------------------------------------------------------
        //  Map.GetStatus() -> state, mapName, progressPercent
        // ------------------------------------------------------------
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