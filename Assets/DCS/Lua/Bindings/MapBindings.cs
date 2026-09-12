using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DynamicComponent.Lua.Bindings
{
    public static class MapBindings
    {
        public static void Register(IntPtr L)
        {
            // Создаем глобальную таблицу Map в Lua
            LuaNative.lua_newtable(L);

            RegisterMethod(L, "Stream", Lua_StreamMap);
            RegisterMethod(L, "Unload", Lua_UnloadMap);
            RegisterMethod(L, "GetStatus", Lua_GetMapStatus);

            LuaNative.lua_setglobal(L, "Map");
        }

        private static void RegisterMethod(IntPtr L, string name, Func<IntPtr, int> fn)
        {
            LuaNative.lua_pushstring(L, name);
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(fn);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_settable(L, -3); // Записываем функцию в таблицу
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_StreamMap(IntPtr L)
        {
            IntPtr strPtr = LuaNative.lua_tolstring(L, 1, IntPtr.Zero);
            string mapName = strPtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(strPtr) : string.Empty;

            if (!string.IsNullOrEmpty(mapName) && SpatialRuntime.Instance != null)
            {
                DatasetLoader.StreamMapAsync(mapName, SpatialRuntime.Instance);
            }
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
            string state = DatasetLoader.IsLoading ? "Loading" : (!string.IsNullOrEmpty(DatasetLoader.CurrentMapName) ? "Loaded" : "Unloaded");

            LuaNative.lua_pushstring(L, state);
            LuaNative.lua_pushstring(L, DatasetLoader.CurrentMapName);

            double progressPercent = Math.Round(DatasetLoader.LoadingProgress * 100.0, 1);
            LuaNative.lua_pushnumber(L, progressPercent);

            return 3;
        }

        /// <summary>
        /// Маршрутизатор пространственных событий и стриминга: C# -> Lua
        /// </summary>
        public static void NotifyZoneEvent(ushort zoneHostId, string eventName)
        {
            if (LuaManager.Instance == null) return;
            IntPtr L = LuaManager.Instance.MainState;
            if (L == IntPtr.Zero) return;

            LuaNative.lua_getglobal(L, "DCS_Spatial_EventRouter");
            if (LuaNative.lua_type(L, -1) == LuaNative.LUA_TFUNCTION)
            {
                LuaNative.lua_pushstring(L, eventName);
                LuaNative.lua_pushinteger(L, zoneHostId);

                if (LuaNative.lua_pcallk(L, 2, 0, 0, 0, IntPtr.Zero) != 0)
                {
                    IntPtr errPtr = LuaNative.lua_tolstring(L, -1, IntPtr.Zero);
                    string error = errPtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(errPtr) : "Unknown Error";
                    Debug.LogError($"[Lua Event Bridge] Ошибка выполнения {eventName}: {error}");
                    LuaNative.lua_pop(L, 1);
                }
            }
            else
            {
                LuaNative.lua_pop(L, 1);
            }
        }

    }
}
