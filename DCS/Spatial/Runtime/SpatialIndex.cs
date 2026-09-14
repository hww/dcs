using System;
using System.Collections.Generic;
using UnityEngine;

namespace DCS.Spatial
{
    public sealed class SpatialIndex
    {
        private readonly float _cellSize;
        private readonly Dictionary<Vector3Int, List<int>> _grid = new Dictionary<Vector3Int, List<int>>();
        private SpatialProxy[] _proxies = Array.Empty<SpatialProxy>();
        private RuntimeGeometryStorage _geometry;

        public SpatialIndex(float cellSize = 15f)
        {
            _cellSize = Mathf.Max(0.1f, cellSize);
        }

        public int ProxyCount => _proxies.Length;

        public void Load(IReadOnlyList<SpatialProxy> proxies, RuntimeGeometryStorage geometry)
        {
            Clear();
            if (proxies == null || geometry == null)
                return;

            _geometry = geometry;
            _proxies = new SpatialProxy[proxies.Count];

            for (int i = 0; i < proxies.Count; i++)
            {
                _proxies[i] = proxies[i];
                RegisterProxy(i, _proxies[i]);
            }
        }

        public void Clear()
        {
            _grid.Clear();
            _proxies = Array.Empty<SpatialProxy>();
            _geometry = null;
        }

        public void QueryPoint(Vector3 point, SpatialQueryFilter filter, List<ushort> results)
        {
            if (results == null)
                return;

            results.Clear();
            if (_geometry == null)
                return;

            Vector3Int cell = WorldToCell(point);
            if (!_grid.TryGetValue(cell, out List<int> candidates))
                return;

            for (int i = 0; i < candidates.Count; i++)
            {
                SpatialProxy proxy = _proxies[candidates[i]];

                if (!MatchesFilter(proxy, filter))
                    continue;

                if (!BoundsContainsPoint(proxy, point))
                    continue;

                if (!GeometryIntersection.IsPointInside(point, proxy.Geometry, _geometry))
                    continue;

                AddUnique(results, proxy.OwnerId);
            }
        }

        public void QueryRadius(Vector3 center, float radius, SpatialQueryFilter filter, List<ushort> results)
        {
            if (results == null)
                return;

            results.Clear();
            if (_geometry == null || radius < 0f)
                return;

            Bounds queryBounds = new Bounds(center, Vector3.one * (radius * 2f));
            Vector3Int minCell = WorldToCell(queryBounds.min);
            Vector3Int maxCell = WorldToCell(queryBounds.max);

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    for (int z = minCell.z; z <= maxCell.z; z++)
                    {
                        Vector3Int cell = new Vector3Int(x, y, z);
                        if (!_grid.TryGetValue(cell, out List<int> candidates))
                            continue;

                        for (int i = 0; i < candidates.Count; i++)
                        {
                            SpatialProxy proxy = _proxies[candidates[i]];

                            if (!MatchesFilter(proxy, filter))
                                continue;

                            if (!BoundsIntersectsSphere(proxy, center, radius))
                                continue;

                            AddUnique(results, proxy.OwnerId);
                        }
                    }
                }
            }
        }

        private bool MatchesFilter(SpatialProxy proxy, SpatialQueryFilter filter)
        {
            if (filter.FilterByType && proxy.ObjectType != filter.ObjectType)
                return false;

            if (filter.FilterByOwner && proxy.OwnerId != filter.OwnerId)
                return false;

            return true;
        }

        private void RegisterProxy(int proxyIndex, SpatialProxy proxy)
        {
            Vector3Int minCell = WorldToCell(proxy.AABBMin);
            Vector3Int maxCell = WorldToCell(proxy.AABBMax);

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    for (int z = minCell.z; z <= maxCell.z; z++)
                    {
                        Vector3Int cell = new Vector3Int(x, y, z);
                        if (!_grid.TryGetValue(cell, out List<int> list))
                        {
                            list = new List<int>();
                            _grid.Add(cell, list);
                        }

                        list.Add(proxyIndex);
                    }
                }
            }
        }

        private Vector3Int WorldToCell(Vector3 position)
        {
            return new Vector3Int(
                Mathf.FloorToInt(position.x / _cellSize),
                Mathf.FloorToInt(position.y / _cellSize),
                Mathf.FloorToInt(position.z / _cellSize));
        }

        private static bool BoundsContainsPoint(SpatialProxy proxy, Vector3 point)
        {
            return point.x >= proxy.AABBMin.x && point.x <= proxy.AABBMax.x &&
                   point.y >= proxy.AABBMin.y && point.y <= proxy.AABBMax.y &&
                   point.z >= proxy.AABBMin.z && point.z <= proxy.AABBMax.z;
        }

        private static bool BoundsIntersectsSphere(SpatialProxy proxy, Vector3 center, float radius)
        {
            float x = Mathf.Clamp(center.x, proxy.AABBMin.x, proxy.AABBMax.x);
            float y = Mathf.Clamp(center.y, proxy.AABBMin.y, proxy.AABBMax.y);
            float z = Mathf.Clamp(center.z, proxy.AABBMin.z, proxy.AABBMax.z);

            float dx = center.x - x;
            float dy = center.y - y;
            float dz = center.z - z;
            return dx * dx + dy * dy + dz * dz <= radius * radius;
        }

        private static void AddUnique(List<ushort> results, ushort ownerId)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i] == ownerId)
                    return;
            }

            results.Add(ownerId);
        }
    }
}
