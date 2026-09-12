using System.Collections.Generic;
using UnityEngine;

namespace DynamicComponent
{
    public class SpatialRuntime : MonoBehaviour
    {
        private static SpatialRuntime _instance;
        public static SpatialRuntime Instance => _instance;

        // Наш изолированный пространственный индекс из Шага 6
        public SpatialIndex Index { get; private set; }

        [SerializeField]
        private float _defaultCellSize = 15f;

        // Буферы для исключения аллокаций памяти во время кадра (0 Garbage Collection)
        private readonly List<ushort> _queryResultBuffer = new List<ushort>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Инициализируем «слепой» индекс
            Index = new SpatialIndex(_defaultCellSize);
        }

        /// <summary>
        /// Шлюз для регистрации геометрии загруженного датасета
        /// </summary>
        public void RegisterSpatialData(List<SpatialShapeRecord> shapes)
        {
            Index.LoadSpatialData(shapes);
            Debug.Log($"[SpatialRuntime] В пространственный индекс успешно загружено примитивов: {shapes.Count}");
        }

        /// <summary>
        /// Полная очистка индекса при выгрузке карты
        /// </summary>
        public void UnregisterSpatialData()
        {
            Index.LoadSpatialData(new List<SpatialShapeRecord>());
            Debug.Log("[SpatialRuntime] Пространственный индекс полностью очищен.");
        }

        // ============================================================
        //  УНИВЕРСАЛЬНЫЙ ИНТЕРФЕЙС ЗАПРОСОВ (ДЛЯ C# И LUA ФАСАДОВ)
        // ============================================================

        /// <summary>
        /// Кто находится в этой точке? (Применяется для проверки зон под игроком/объектом)
        /// </summary>
        public List<ushort> GetObjectsAtPoint(Vector3 position, EObjectType type)
        {
            Index.QueryAllAtPoint(position, type, _queryResultBuffer);
            return _queryResultBuffer;
        }

        /// <summary>
        /// Что находится рядом в радиусе? (Применяется для поиска укрытий, зацепов ИИ и пулями)
        /// </summary>
        public List<ushort> GetObjectsInRadius(Vector3 center, float radius, EObjectType type)
        {
            Index.QueryAllInRadius(center, radius, type, _queryResultBuffer);
            return _queryResultBuffer;
        }
    }
}
