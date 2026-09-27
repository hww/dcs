using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using DCS.Data;

namespace DCS.World
{
    /// <summary>
    /// Загружает сцену и её MapDataset.
    /// Не знает ни про Root, ни про WorldRuntime, ни про SpatialDomain.
    /// Кто хочет — получает MapDataset через onComplete.
    /// </summary>
    public static class DatasetLoader
    {
        private static MapDataset _currentDataset;
        private static string _loadingMapName;
        private static bool _isLoading;
        private static float _loadingProgress;

        public static string CurrentMapName => _currentDataset != null ? _currentDataset.MapName : string.Empty;
        public static bool IsLoading => _isLoading;
        public static float LoadingProgress => _loadingProgress;

        /// <summary>
        /// Асинхронно загружает сцену и датасет.
        /// onComplete получает загруженный MapDataset. Root сам решает,
        /// что с ним делать (World.Load, SpatialDomain.StaticSpatial.Load, ...).
        /// </summary>
        public static void StreamMapAsync(
            string mapName,
            MonoBehaviour coroutineRunner,
            Action<MapDataset> onComplete = null)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[DatasetLoader] Загрузка карты уже идет: {_loadingMapName}. Запрос '{mapName}' отклонен.");
                return;
            }

            coroutineRunner.StartCoroutine(StreamMapRoutine(mapName, onComplete));
        }

        private static IEnumerator StreamMapRoutine(string mapName, Action<MapDataset> onComplete)
        {
            _isLoading = true;
            _loadingProgress = 0f;
            _loadingMapName = mapName;
            Debug.Log($"<color=cyan>[DatasetLoader]</color> Начат асинхронный стриминг: {mapName}");

            // --- ЭТАП 1: Выгрузка старой карты ---
            if (_currentDataset != null)
            {
                string oldMapName = _currentDataset.MapName;
                UnloadCurrent();

                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(oldMapName);
                if (unloadOp != null)
                {
                    while (!unloadOp.isDone)
                    {
                        _loadingProgress = unloadOp.progress * 0.1f;
                        yield return null;
                    }
                }
            }

            // --- ЭТАП 2: Загрузка сцены ---
            AsyncOperation loadSceneOp = SceneManager.LoadSceneAsync(mapName, LoadSceneMode.Additive);
            if (loadSceneOp == null)
            {
                Debug.LogError($"[DatasetLoader] Не удалось загрузить сцену: {mapName}");
                _isLoading = false;
                yield break;
            }

            while (!loadSceneOp.isDone)
            {
                _loadingProgress = 0.1f + (loadSceneOp.progress / 0.9f) * 0.8f;
                yield return null;
            }

            Scene loadedScene = SceneManager.GetSceneByName(mapName);
            if (loadedScene.IsValid())
                SceneManager.SetActiveScene(loadedScene);

            // --- ЭТАП 3: Загрузка датасета ---
            string resourcePath = $"Maps/{mapName}_dataset";
            _currentDataset = Resources.Load<MapDataset>(resourcePath);

            if (_currentDataset == null)
            {
                Debug.LogError($"[DatasetLoader] Датасет не найден: Resources/{resourcePath}");
                _isLoading = false;
                yield break;
            }

            _loadingProgress = 1.0f;
            _isLoading = false;
            Debug.Log($"<color=green>[DatasetLoader]</color> Стриминг локации '{mapName}' завершен.");

            // Root сам вызовет World.Load(dataset, spatialDomain).
            onComplete?.Invoke(_currentDataset);
        }

        /// <summary>
        /// Выгружает текущий датасет из памяти. Root должен сам очистить
        /// WorldRuntime и SpatialDomain.StaticSpatial — DatasetLoader про них не знает.
        /// </summary>
        public static void UnloadCurrent()
        {
            if (_currentDataset == null) return;
            Resources.UnloadAsset(_currentDataset);
            _currentDataset = null;
        }
    }
}