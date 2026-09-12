using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using DynamicComponent.Lua.Bindings;

namespace DynamicComponent
{
    public static class DatasetLoader
    {
        private static MapDataset _currentDataset;
        private static string _loadingMapName;
        private static bool _isLoading;
        private static float _loadingProgress; // Переменная от 0.0 до 1.0

        public static string CurrentMapName => _currentDataset != null ? _currentDataset.MapName : string.Empty;
        public static bool IsLoading => _isLoading;

        /// <summary>
        /// Текущий прогресс загрузки в диапазоне от 0.0 до 1.0 (для Lua)
        /// </summary>
        public static float LoadingProgress => _loadingProgress;

        public static void StreamMapAsync(string mapName, MonoBehaviour coroutineRunner, Action onComplete = null)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[DatasetLoader] Загрузка карты уже идет: {_loadingMapName}. Запрос '{mapName}' отклонен.");
                return;
            }

            coroutineRunner.StartCoroutine(StreamMapRoutine(mapName, onComplete));
        }

        private static IEnumerator StreamMapRoutine(string mapName, Action onComplete)
        {
            _isLoading = true;
            _loadingProgress = 0f;
            _loadingMapName = mapName;
            Debug.Log($"<color=cyan>[Dataseter Stream]</color> Начат асинхронный стриминг: {mapName}");

            // --- ЭТАП 1: Выгрузка старой карты ---
            if (_currentDataset != null)
            {
                string oldMapName = _currentDataset.MapName;
                UnloadMapDataset();

                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(oldMapName);
                if (unloadOp != null)
                {
                    while (!unloadOp.isDone)
                    {
                        // Во время выгрузки прогресс держим около нуля
                        _loadingProgress = unloadOp.progress * 0.1f;
                        yield return null;
                    }
                }
            }

            // --- ЭТАП 2: Асинхронная загрузка визуала Unity ---
            AsyncOperation loadSceneOp = SceneManager.LoadSceneAsync(mapName, LoadSceneMode.Additive);
            if (loadSceneOp == null)
            {
                Debug.LogError($"[DatasetLoader] Не удалось загрузить сцену: {mapName}");
                _isLoading = false;
                yield break;
            }

            while (!loadSceneOp.isDone)
            {
                // Unity загружает меши и текстуры. Прогресс операции идет от 0.0 до 0.9.
                // Нормализуем его, чтобы для Lua это выглядело как честные 0% - 90%
                _loadingProgress = 0.1f + (loadSceneOp.progress / 0.9f) * 0.8f;
                yield return null;
            }

            Scene loadedScene = SceneManager.GetSceneByName(mapName);
            if (loadedScene.IsValid())
            {
                SceneManager.SetActiveScene(loadedScene);
            }

            // --- ЭТАП 3: Загрузка логических данных (DCS Dataset) ---
            string resourcePath = $"Maps/{mapName}_dataset";
            _currentDataset = Resources.Load<MapDataset>(resourcePath);

            if (_currentDataset == null)
            {
                Debug.LogError($"[DatasetLoader] Датасет не найден: Resources/{resourcePath}");
                _isLoading = false;
                yield break;
            }

            if (SpatialRuntime.Instance != null)
            {
                SpatialRuntime.Instance.RegisterSpatialData(_currentDataset);
            }

            RegisterEntitiesInComponentPools(_currentDataset);

            // Загрузка завершена на 100%
            _loadingProgress = 1.0f;
            _isLoading = false;
            Debug.Log($"<color=green>[Dataseter Stream]</color> Стриминг локации '{mapName}' завершен.");

            onComplete?.Invoke();
            MapBindings.NotifyZoneEvent(0, "OnMapStreamingFinished");
        }

        public static void UnloadMapDataset()
        {
            if (_currentDataset == null) return;

            if (SpatialRuntime.Instance != null)
            {
                SpatialRuntime.Instance.UnregisterSpatialData();
            }

            UnregisterEntitiesFromPools(_currentDataset);
            Resources.UnloadAsset(_currentDataset);
            _currentDataset = null;
        }

        private static void RegisterEntitiesInComponentPools(MapDataset dataset) { }
        private static void UnregisterEntitiesFromPools(MapDataset dataset) { }
    }
}
