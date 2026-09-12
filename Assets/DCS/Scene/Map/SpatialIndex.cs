using System.Collections.Generic;
using UnityEngine;

namespace DynamicComponent
{
    public class SpatialIndex
    {
        private readonly float _cellSize;

        // Сетка Broadphase: Координаты ячейки -> Список индексов примитивов в плоском массиве _shapes
        private readonly Dictionary<Vector3Int, List<int>> _grid = new Dictionary<Vector3Int, List<int>>();

        // Плоский кэш-массив всех геометрических примитивов (Narrowphase)
        private SpatialShapeRecord[] _shapes = new SpatialShapeRecord[0];

        public SpatialIndex(float cellSize = 15f)
        {
            _cellSize = cellSize;
        }

        /// <summary>
        /// Полная инициализация геометрии при загрузке датасета карты
        /// </summary>
        public void LoadSpatialData(List<SpatialShapeRecord> shapesData)
        {
            _grid.Clear();
            _shapes = shapesData.ToArray();

            // Регистрируем каждый примитив в пространственную хэш-сетку по его запеченному честному AABB
            for (int i = 0; i < _shapes.Length; i++)
            {
                RegisterShapeInGrid(i, ref _shapes[i]);
            }
        }

        private void RegisterShapeInGrid(int shapeIndex, ref SpatialShapeRecord shape)
        {
            // Берем честные мировые AABB, рассчитанные оффлайн в редакторе
            Vector3Int minCell = WorldToCell(shape.AABBMin);
            Vector3Int maxCell = WorldToCell(shape.AABBMax);

            // Записываем индекс примитива во все ячейки пространства, которые он пересекает
            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    for (int z = minCell.z; z <= maxCell.z; z++)
                    {
                        Vector3Int cellCoords = new Vector3Int(x, y, z);
                        if (!_grid.TryGetValue(cellCoords, out List<int> cellList))
                        {
                            cellList = new List<int>();
                            _grid[cellCoords] = cellList;
                        }
                        cellList.Add(shapeIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Универсальный поисковый запрос: Найти все уникальные OwnerId объектов определенного типа в точке пространства.
        /// Возвращает количество найденных объектов (0 аллокаций памяти во время кадра!).
        /// </summary>
        public int QueryAllAtPoint(Vector3 point, EObjectType filterType, List<ushort> outResults)
        {
            outResults.Clear();
            Vector3Int cellCoords = WorldToCell(point);

            if (!_grid.TryGetValue(cellCoords, out List<int> cellElements))
            {
                return 0;
            }

            for (int i = 0; i < cellElements.Count; i++)
            {
                int shapeIdx = cellElements[i];
                ref var shape = ref _shapes[shapeIdx];

                // Быстрый Broadphase-фильтр по слою
                if (shape.ObjectType != filterType) continue;

                // Если этот логический объект уже добавлен в результаты (через другой свой примитив) — скипаем
                if (outResults.Contains(shape.OwnerId)) continue;

                // Точечный Narrowphase-тест тригонометрии конкретной формы
                if (GeometryIntersection.IsPointInside(point, ref shape))
                {
                    outResults.Add(shape.OwnerId);
                }
            }

            return outResults.Count;
        }

        /// <summary>
        /// Универсальный поисковый запрос в радиусе (например, для поиска ближайших укрытий или крюков зацепа)
        /// </summary>
        public int QueryAllInRadius(Vector3 center, float radius, EObjectType filterType, List<ushort> outResults)
        {
            outResults.Clear();

            Vector3Int minCell = WorldToCell(center - new Vector3(radius, radius, radius));
            Vector3Int maxCell = WorldToCell(center + new Vector3(radius, radius, radius));
            float sqrRadius = radius * radius;

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    for (int z = minCell.z; z <= maxCell.z; z++)
                    {
                        if (_grid.TryGetValue(new Vector3Int(x, y, z), out List<int> cellElements))
                        {
                            for (int i = 0; i < cellElements.Count; i++)
                            {
                                int shapeIdx = cellElements[i];
                                ref var shape = ref _shapes[shapeIdx];

                                if (shape.ObjectType != filterType) continue;
                                if (outResults.Contains(shape.OwnerId)) continue;

                                // Для поиска в радиусе: проверяем расстояние от центра примитива 
                                // (или можно усложнить до расстояния до AABB, но для MVP — расстояние до центра)
                                Vector3 primCenter = (shape.GeomType == EGeometryType.Triangle) ? shape.RawData0 : shape.RawData0;
                                if ((primCenter - center).sqrMagnitude <= sqrRadius)
                                {
                                    outResults.Add(shape.OwnerId);
                                }
                            }
                        }
                    }
                }
            }

            return outResults.Count;
        }

        private Vector3Int WorldToCell(Vector3 worldPos)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPos.x / _cellSize),
                Mathf.FloorToInt(worldPos.y / _cellSize),
                Mathf.FloorToInt(worldPos.z / _cellSize)
            );
        }
    }
}
