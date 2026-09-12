using System;
using System.Collections.Generic;
using UnityEngine;


namespace DynamicComponent
{

    public sealed class SpatialIndex
    {
        private readonly float _cellSize;

        private readonly Dictionary<Vector3Int, List<int>> _grid =
            new Dictionary<Vector3Int, List<int>>();

        private SpatialProxy[] _proxies = Array.Empty<SpatialProxy>();

        private RuntimeGeometryStorage _geometry;

        public SpatialIndex(float cellSize = 15f)
        {
            _cellSize = Mathf.Max(0.1f, cellSize);
        }

        public int ProxyCount => _proxies.Length;

        public void Load(
            IReadOnlyList<SpatialProxy> proxies,
            RuntimeGeometryStorage geometry)
        {
            Clear();

            if (proxies == null || geometry == null)
                return;

            _geometry = geometry;

            _proxies = new SpatialProxy[proxies.Count];

            for (int i = 0; i < proxies.Count; i++)
            {
                SpatialProxy proxy = proxies[i];

                _proxies[i] = proxy;

                RegisterProxy(i, proxy);
            }
        }

        public void Clear()
        {
            _grid.Clear();

            _proxies = Array.Empty<SpatialProxy>();

            _geometry = null;
        }

        public void QueryPoint(
            Vector3 point,
            EObjectType objectType,
            List<ushort> results)
        {
            if (results == null)
                return;

            results.Clear();

            if (_geometry == null)
                return;

            Vector3Int cell = WorldToCell(point);

            if (!_grid.TryGetValue(
                    cell,
                    out List<int> candidates))
            {
                return;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                int proxyIndex = candidates[i];

                SpatialProxy proxy = _proxies[proxyIndex];

                if (proxy.ObjectType != objectType)
                    continue;

                if (!BoundsContainsPoint(proxy, point))
                    continue;

                if (!GeometryIntersection.IsPointInside(
                        point,
                        proxy.Geometry, // Было proxy.GeometryHandle
                        _geometry))
                {
                    continue;
                }

                AddUnique(
                    results,
                    proxy.OwnerId);
            }
        }

        public void QueryRadius(
            Vector3 center,
            float radius,
            EObjectType objectType,
            List<ushort> results)
        {
            if (results == null)
                return;

            results.Clear();

            if (_geometry == null || radius < 0f)
                return;

            Bounds queryBounds = new Bounds(
                center,
                Vector3.one * (radius * 2f));

            Vector3Int minCell =
                WorldToCell(queryBounds.min);

            Vector3Int maxCell =
                WorldToCell(queryBounds.max);

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    for (int z = minCell.z; z <= maxCell.z; z++)
                    {
                        Vector3Int cell =
                            new Vector3Int(x, y, z);

                        if (!_grid.TryGetValue(
                                cell,
                                out List<int> candidates))
                        {
                            continue;
                        }

                        for (int i = 0; i < candidates.Count; i++)
                        {
                            int proxyIndex = candidates[i];

                            SpatialProxy proxy =
                                _proxies[proxyIndex];

                            if (proxy.ObjectType != objectType)
                                continue;

                            if (!BoundsIntersectsSphere(
                                    proxy,
                                    center,
                                    radius))
                            {
                                continue;
                            }

                            AddUnique(
                                results,
                                proxy.OwnerId);
                        }
                    }
                }
            }
        }

        private void RegisterProxy(
            int proxyIndex,
            SpatialProxy proxy)
        {
            Vector3Int minCell =
                WorldToCell(proxy.AABBMin);

            Vector3Int maxCell =
                WorldToCell(proxy.AABBMax);

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    for (int z = minCell.z; z <= maxCell.z; z++)
                    {
                        Vector3Int cell =
                            new Vector3Int(x, y, z);

                        if (!_grid.TryGetValue(
                                cell,
                                out List<int> list))
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

        private static bool BoundsContainsPoint(
            SpatialProxy proxy,
            Vector3 point)
        {
            return point.x >= proxy.AABBMin.x &&
                   point.x <= proxy.AABBMax.x &&
                   point.y >= proxy.AABBMin.y &&
                   point.y <= proxy.AABBMax.y &&
                   point.z >= proxy.AABBMin.z &&
                   point.z <= proxy.AABBMax.z;
        }

        private static bool BoundsIntersectsSphere(
            SpatialProxy proxy,
            Vector3 center,
            float radius)
        {
            float closestX =
                Mathf.Max(
                    proxy.AABBMin.x,
                    Mathf.Min(
                        center.x,
                        proxy.AABBMax.x));

            float closestY =
                Mathf.Max(
                    proxy.AABBMin.y,
                    Mathf.Min(
                        center.y,
                        proxy.AABBMax.y));

            float closestZ =
                Mathf.Max(
                    proxy.AABBMin.z,
                    Mathf.Min(
                        center.z,
                        proxy.AABBMax.z));

            float dx = center.x - closestX;
            float dy = center.y - closestY;
            float dz = center.z - closestZ;

            return
                dx * dx +
                dy * dy +
                dz * dz <=
                radius * radius;
        }

        private static void AddUnique(
            List<ushort> results,
            ushort ownerId)
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
