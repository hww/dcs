using System.Collections.Generic;
using DCS.Gameplay;
using UnityEngine;

namespace DCS.Spatial
{
    public sealed class SpatialRuntime : MonoBehaviour
    {
        public static SpatialRuntime Instance { get; private set; }

        [SerializeField]
        private float _cellSize = 15f;

        private readonly List<ushort> _scratch = new List<ushort>();
        private SpatialIndex _index;
        private RuntimeGeometryStorage _geometry;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _geometry = new RuntimeGeometryStorage();
            _index = new SpatialIndex(_cellSize);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void RegisterSpatialData(MapDataset dataset)
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

        public void UnregisterSpatialData()
        {
            _index.Clear();
            _geometry.Clear();
        }

        public void GetObjectsAtPoint(Vector3 point, SpatialQueryFilter filter, List<ushort> results)
        {
            _index.QueryPoint(point, filter, results);
        }

        public void GetObjectsInRadius(Vector3 center, float radius, SpatialQueryFilter filter, List<ushort> results)
        {
            _index.QueryRadius(center, radius, filter, results);
        }

        public bool IsOwnerAtPoint(Vector3 point, ESpatialObjectType type, ushort ownerId)
        {
            _scratch.Clear();
            _index.QueryPoint(point, SpatialQueryFilter.ByType(type), _scratch);

            for (int i = 0; i < _scratch.Count; i++)
            {
                if (_scratch[i] == ownerId)
                    return true;
            }

            return false;
        }
    }
}
