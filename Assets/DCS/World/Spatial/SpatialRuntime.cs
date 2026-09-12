using System.Collections.Generic;
using UnityEngine;
namespace DynamicComponent
{
    public sealed class SpatialRuntime : MonoBehaviour
    {
        public static SpatialRuntime Instance { get; private set; }

        [SerializeField]
        private float _cellSize = 15f;

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

        public void RegisterSpatialData(MapDataset dataset)
        {
            if (dataset == null)
            {
                Debug.LogError(
                    "[SpatialRuntime] Dataset is null.");

                return;
            }

            _geometry.Load(dataset);

            _index.Load(
                dataset.SpatialProxies,
                _geometry);

            Debug.Log(
                $"[SpatialRuntime] Loaded " +
                $"{dataset.SpatialProxies.Count} spatial proxies.");
        }

        public void UnregisterSpatialData()
        {
            _index.Clear();

            _geometry.Clear();
        }

        public void GetObjectsAtPoint(
            Vector3 point,
            EObjectType objectType,
            List<ushort> results)
        {
            _index.QueryPoint(
                point,
                objectType,
                results);
        }

        public void GetObjectsInRadius(
            Vector3 center,
            float radius,
            EObjectType objectType,
            List<ushort> results)
        {
            _index.QueryRadius(
                center,
                radius,
                objectType,
                results);
        }
    }
}