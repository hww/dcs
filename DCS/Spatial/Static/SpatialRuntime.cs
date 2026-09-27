using DCS.Data;
using System.Collections.Generic;
using UnityEngine;

namespace DCS.Spatial
{
    /// <summary>
    /// Запечённый (статический) spatial одного уровня.
    /// Не MonoBehaviour, не синглтон.
    /// Живёт в SpatialDomain.StaticSpatial.
    ///
    /// Один SpatialRuntime = один загруженный датасет.
    /// Load() перезаписывает. Clear() — очищает.
    /// </summary>
    public sealed class SpatialRuntime
    {
        private SpatialIndex _index;
        private RuntimeGeometryStorage _geometry;

        public ushort LevelId { get; }
        public bool HasData => _index != null && _index.ProxyCount > 0;
        public int ProxyCount => _index == null ? 0 : _index.ProxyCount;

        public SpatialRuntime(ushort levelId = 0, float cellSize = 15f)
        {
            LevelId = levelId;
            _geometry = new RuntimeGeometryStorage();
            _index = new SpatialIndex(cellSize);
        }

        public void Load(MapDataset dataset)
        {
            if (dataset == null)
            {
                Debug.LogError("[SpatialRuntime] Dataset is null.");
                return;
            }
            _geometry.Load(
                dataset.Spheres.ToArray(),
                dataset.Boxes.ToArray(),
                dataset.Cylinders.ToArray(),
                dataset.Polygons.ToArray(),
                dataset.PolygonPoints.ToArray());
            _index.Load(dataset.SpatialProxies, _geometry);
        }

        public void Clear()
        {
            _index?.Clear();
            _geometry?.Clear();
        }

        public void QueryPoint(Vector3 point, SpatialQueryFilter filter, List<ushort> owners)
        {
            if (owners == null) return;
            owners.Clear();
            _index?.QueryPoint(point, filter, owners);
        }

        public void QueryRadius(Vector3 center, float radius, SpatialQueryFilter filter, List<ushort> owners)
        {
            if (owners == null) return;
            owners.Clear();
            _index?.QueryRadius(center, radius, filter, owners);
        }

        public bool QueryNearest(Vector3 point, float maxDistance, SpatialQueryFilter filter, out SpatialHit hit)
        {
            hit = default;
            return _index != null && _index.QueryNearest(point, maxDistance, filter, out hit);
        }

        public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, SpatialQueryFilter filter, out SpatialHit hit)
        {
            hit = default;
            return _index != null && _index.Raycast(origin, direction, maxDistance, filter, out hit);
        }

        public bool Contains(Vector3 point, ushort ownerId, ESpatialObjectType type)
            => _index != null && _index.Contains(point, SpatialQueryFilter.ByTypeAndOwner(type, ownerId));
    }
}