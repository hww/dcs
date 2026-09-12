using System;
using System.Collections.Generic;
using UnityEngine;
using DynamicComponent.Lua;
using UnityEngine.SceneManagement; // Подключение вашего Lua-ядра

namespace DynamicComponent
{
    public static class LuaSpatialBridge
    {
        /// <summary>
        /// Маршрутизатор пространственных событий: C# -> Lua
        /// </summary>
        public static void NotifyZoneEvent(ushort zoneHostId, string eventName)
        {
            if (LuaManager.Instance == null) return;
            IntPtr L = LuaManager.Instance.MainState;
            if (L == IntPtr.Zero) return;

            // Вызываем в Lua глобальный распределитель пространственных событий
            LuaNative.lua_getglobal(L, "DCS_Spatial_EventRouter");
            if (LuaNative.lua_type(L, -1) == LuaNative.LUA_TFUNCTION)
            {
                LuaNative.lua_pushstring(L, eventName);
                LuaNative.lua_pushinteger(L, zoneHostId);

                // Безопасный вызов Lua-функции (pcall) с отловом ошибок
                if (LuaNative.lua_pcallk(L, 2, 0, 0, 0, IntPtr.Zero) != 0)
                {
                    IntPtr errPtr = LuaNative.lua_tolstring(L, -1, IntPtr.Zero);
                    string error = errPtr != IntPtr.Zero ? System.Runtime.InteropServices.Marshal.PtrToStringUTF8(errPtr) : "Unknown Error";
                    Debug.LogError($"[Lua Spatial Bridge] Ошибка выполнения {eventName} для Зоны {zoneHostId}: {error}");
                    LuaNative.lua_pop(L, 1); // Удаляем ошибку из стека
                }
            }
            else
            {
                LuaNative.lua_pop(L, 1); // Удаляем из стека то, что не является функцией
            }
        }

        // ============================================================
        //  РЕГИСТРАЦИЯ ФУНКЦИЙ ДЛЯ ВЫЗОВА ИЗ LUA (LUA -> C#)
        // ============================================================

        /// <summary>
        /// C#-делегат для экспорта в Lua: позволяет скриптам искать объекты вокруг точки.
        /// Пример в Lua: local items = Spatial.QueryNearby(x, y, z, radius, objectType)
        /// </summary>
        public static int Lua_QueryNearby(IntPtr L)
        {
            // Извлекаем аргументы из Lua-стека с учетом сигнатур lua_tonumberx и lua_tointegerx
            float x = (float)LuaNative.lua_tonumberx(L, 1, IntPtr.Zero);
            float y = (float)LuaNative.lua_tonumberx(L, 2, IntPtr.Zero);
            float z = (float)LuaNative.lua_tonumberx(L, 3, IntPtr.Zero);
            float radius = (float)LuaNative.lua_tonumberx(L, 4, IntPtr.Zero);
            byte objTypeByte = (byte)LuaNative.lua_tointegerx(L, 5, IntPtr.Zero);

            EObjectType filterType = (EObjectType)objTypeByte;
            Vector3 queryPos = new Vector3(x, y, z);

            // Запрашиваем данные у нашего «глупого» пространственного рантайма
            List<ushort> results = new List<ushort>(); // Временный список для Lua-стека (вызывается редко)
            if (SpatialRuntime.Instance != null)
            {
                SpatialRuntime.Instance.GetObjectsInRadius(queryPos, radius, filterType, results);
            }

            // Создаем массив-таблицу в Lua
            LuaNative.lua_newtable(L);
            for (int i = 0; i < results.Count; i++)
            {
                // Эквивалент lua_rawseti(L, -2, i + 1) средствами доступного API:
                LuaNative.lua_pushinteger(L, i + 1);       // Ключ таблицы (индексация с 1)
                LuaNative.lua_pushinteger(L, results[i]);  // Значение (OwnerId)
                LuaNative.lua_settable(L, -3);             // Записываем в таблицу на индексе -3
            }

            return 1; // Возвращаем одну таблицу как результат функции в Lua
        }

        // ============================================================
        //  МЕНЕДЖМЕНТ КАРТ ИЗ LUA (LUA -> C#)
        // ============================================================

        /// <summary>
        /// Lua API: Начать асинформный стриминг новой сцены.
        /// Пример в Lua: Map.Stream("SwampZone")
        /// </summary>
        public static int Lua_StreamMap(IntPtr L)
        {
            // Получаем имя карты из первого аргумента Lua-стека
            IntPtr strPtr = LuaNative.lua_tolstring(L, 1, IntPtr.Zero);
            string mapName = strPtr != IntPtr.Zero ? System.Runtime.InteropServices.Marshal.PtrToStringUTF8(strPtr) : string.Empty;

            if (string.IsNullOrEmpty(mapName))
            {
                return LuaNative.luaL_error(L, "Map.Stream: Имя карты не может быть пустым!");
            }

            // Запускаем корутину на синглтоне SpatialRuntime
            if (SpatialRuntime.Instance != null)
            {
                DatasetLoader.StreamMapAsync(mapName, SpatialRuntime.Instance);
            }

            return 0; // Ничего не возвращаем в Lua, процесс пошел асинхронно
        }

        /// <summary>
        /// Lua API: Полностью выгрузить текущую карту и очистить память.
        /// Пример в Lua: Map.Unload()
        /// </summary>
        public static int Lua_UnloadMap(IntPtr L)
        {
            string currentMap = DatasetLoader.CurrentMapName;
            if (!string.IsNullOrEmpty(currentMap))
            {
                DatasetLoader.UnloadMapDataset();
                // Также асинхронно выгружаем саму визуальную сцену
                SceneManager.UnloadSceneAsync(currentMap);
            }
            return 0;
        }

        /// <summary>
        /// Lua API: Запросить состояние стриминга и текущий прогресс.
        /// Пример в Lua: local state, mapName, progress = Map.GetStatus()
        /// </summary>
        public static int Lua_GetMapStatus(IntPtr L)
        {
            string state = "Unloaded";
            if (DatasetLoader.IsLoading)
            {
                state = "Loading";
            }
            else if (!string.IsNullOrEmpty(DatasetLoader.CurrentMapName))
            {
                state = "Loaded";
            }

            // Возвращаем 3 значения в стек Lua подряд:
            // 1. Состояние ("Unloaded", "Loading", "Loaded")
            LuaNative.lua_pushstring(L, state);

            // 2. Имя активной карты (или пустая строка)
            string mapName = DatasetLoader.CurrentMapName;
            LuaNative.lua_pushstring(L, mapName);

            // 3. Процент прогресса от 0 до 100
            double progressPercent = System.Math.Round(DatasetLoader.LoadingProgress * 100.0, 1);
            LuaNative.lua_pushnumber(L, progressPercent);

            return 3; // Сообщаем Lua, что на стек положено ровно 3 значения
        }
    }
}
