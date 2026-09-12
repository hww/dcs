using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace DynamicComponent
{
    public static class DatasetLoader
    {
        private static MapDataset _currentDataset;
        public static string CurrentMapName => _currentDataset != null ? _currentDataset.MapName : string.Empty;

        /// <summary>
        /// Загрузить скомпилированный датасет и привязать его данные к рантайму
        /// </summary>
        public static void LoadMapDataset(string mapName)
        {
            // 1. Пытаемся загрузить ассет, созданный оффлайн-компилятором WorldPacker
            string resourcePath = $"Maps/{mapName}_dataset";
            _currentDataset = Resources.Load<MapDataset>(resourcePath);

            if (_currentDataset == null)
            {
                Debug.LogError($"[DatasetLoader] Не удалось найти скомпилированный датасет по пути: Resources/{resourcePath}");
                return;
            }

            Debug.Log($"[DatasetLoader] Скомпилированный датасет для '{mapName}' успешно считан из памяти.");

            // 2. Отправляем плоскую геометрию в пространственный индекс
            if (SpatialRuntime.Instance != null)
            {
                SpatialRuntime.Instance.RegisterSpatialData(_currentDataset.SpatialShapes);
            }

            // 3. Регистрируем метаданные сущностей в пулы компонентов (Component Pools)
            RegisterEntitiesInComponentPools(_currentDataset);
        }

        /// <summary>
        /// Начисто выгрузить датасет из рантайма
        /// </summary>
        public static void UnloadMapDataset()
        {
            if (_currentDataset == null) return;

            // 1. Очищаем пространственную геометрию
            if (SpatialRuntime.Instance != null)
            {
                SpatialRuntime.Instance.UnregisterSpatialData();
            }

            // 2. Вычищаем ID сущностей из глобальных пулов компонентов
            UnregisterEntitiesFromPools(_currentDataset);

            // 3. Освобождаем память ассета
            Resources.UnloadAsset(_currentDataset);
            _currentDataset = null;
        }

        private static void RegisterEntitiesInComponentPools(MapDataset dataset)
        {
            // Здесь ваши рантайм-системы (ComponentRegistry / PositionPool) берут сухие записи:
            // dataset.Zones, dataset.Spawners, dataset.Locators
            // и регистрируют их в плоские массивы симуляции под их HostId.
            Debug.Log($"[DatasetLoader] Метаданные сущностей зарегистрированы в пулы. Зон: {dataset.Zones.Count}, Спаунеров: {dataset.Spawners.Count}");
        }

        private static void UnregisterEntitiesFromPools(MapDataset dataset)
        {
            // Вычищаем из пулов симуляции все HostId, принадлежавшие выгружаемой карте
            Debug.Log("[DatasetLoader] Метаданные сущностей удалены из пулов симуляции.");
        }
    }
}
