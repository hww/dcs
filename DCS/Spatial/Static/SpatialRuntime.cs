using System.Collections.Generic;
using DCS.Gameplay;
using UnityEngine;

namespace DCS.Spatial
{
    /// <summary>Owns the active compiled spatial dataset and exposes allocation-free-by-caller query contracts.</summary>
    public sealed class SpatialRuntime : MonoBehaviour
    {
        public static SpatialRuntime Instance { get; private set; }
        [SerializeField] private float _cellSize = 15f;
        private SpatialIndex _index;
        private RuntimeGeometryStorage _geometry;
        public ushort DatasetId { get; private set; }
        public bool HasData => _index != null && _index.ProxyCount > 0;
        public int ProxyCount => _index == null ? 0 : _index.ProxyCount;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            _geometry = new RuntimeGeometryStorage(); _index = new SpatialIndex(_cellSize);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Load(MapDataset dataset, ushort datasetId = 0)
        {
            if (dataset == null) { Debug.LogError("[SpatialRuntime] Dataset is null."); return; }
            DatasetId = datasetId;
            _geometry.Load(dataset.Spheres.ToArray(), dataset.Boxes.ToArray(), dataset.Cylinders.ToArray(), dataset.Polygons.ToArray(), dataset.PolygonPoints.ToArray());
            _index.Load(dataset.SpatialProxies, _geometry);
        }

        public void Clear()
        {
            DatasetId = 0; _index?.Clear(); _geometry?.Clear();
        }

        public void QueryPoint(
            Vector3 point,
            SpatialQueryFilter filter,
            List<ushort> owners)
        {
            if (owners == null)
                return;

            owners.Clear();

            if (_index == null)
                return;

            _index.QueryPoint(
                point,
                filter,
                owners);
        }

        public void QueryRadius(
            Vector3 center,
            float radius,
            SpatialQueryFilter filter,
            List<ushort> owners)
        {
            if (owners == null)
                return;

            owners.Clear();

            if (_index == null)
                return;

            _index.QueryRadius(
                center,
                radius,
                filter,
                owners);
        }
        public bool QueryNearest(Vector3 point, float maxDistance, SpatialQueryFilter filter, out SpatialHit hit) 
        {
            hit = default;

            if (_index == null)
                return false;

            return _index.QueryNearest(
                point,
                maxDistance,
                filter,
                out hit);
        }
        public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, SpatialQueryFilter filter, out SpatialHit hit)
        {
            hit = default;

            if (_index == null)
                return false;

            return _index.Raycast(
                origin,
                direction,
                maxDistance,
                filter,
                out hit);
        }
        public bool Contains(Vector3 point, ushort ownerId, ESpatialObjectType type) => _index != null && _index.Contains(point, SpatialQueryFilter.ByTypeAndOwner(type, ownerId));

    }
}
