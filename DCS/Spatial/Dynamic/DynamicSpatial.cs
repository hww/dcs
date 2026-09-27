using System.Collections.Generic;
using UnityEngine;

namespace DCS.Spatial
{
    /// <summary>
    /// Uniform grid for dynamic objects.
    /// Key is Host.Id, not ActorHandle. Position is provided by caller
    /// (usually read from PositionComponent).
    /// </summary>
    public sealed class DynamicSpatial
    {
        private readonly int _gridWidth, _gridHeight, _gridDepth;
        private readonly int _cellCount;
        private readonly float _cellSize;
        private readonly Cell[] _cells;

        private Entry[] _entries;
        private int _entriesCount;
        private readonly Dictionary<ushort, int> _hostToEntry;

        private struct Entry
        {
            public ushort HostId;
            public ESpatialObjectType ObjectType;
            public Vector3 AABBMin;
            public Vector3 AABBMax;
            public int CellIndex;
            public int NextInCell;
            public bool Active;
        }

        private sealed class Cell
        {
            public int Head = -1;
            public int Count;
        }

        public DynamicSpatial(int width, int height, int depth, float cellSize,
                              int initialEntryCapacity = 512)
        {
            _gridWidth = Mathf.Max(1, width);
            _gridHeight = Mathf.Max(1, height);
            _gridDepth = Mathf.Max(1, depth);
            _cellSize = Mathf.Max(0.1f, cellSize);
            _cellCount = _gridWidth * _gridHeight * _gridDepth;
            _cells = new Cell[_cellCount];
            for (int i = 0; i < _cellCount; i++) _cells[i] = new Cell();
            _entries = new Entry[initialEntryCapacity];
            _entriesCount = 0;
            _hostToEntry = new Dictionary<ushort, int>(initialEntryCapacity);
        }

        public int ActiveCount => _hostToEntry.Count;

        public void Register(ushort hostId, ESpatialObjectType type,
                             Vector3 aabbMin, Vector3 aabbMax)
        {
            if (_hostToEntry.ContainsKey(hostId)) return;

            Vector3 center = (aabbMin + aabbMax) * 0.5f;
            int cellIdx = WorldToCellIndex(center);

            if (_entriesCount >= _entries.Length)
                System.Array.Resize(ref _entries, _entries.Length * 2);

            int idx = _entriesCount++;
            ref var e = ref _entries[idx];
            e.HostId = hostId;
            e.ObjectType = type;
            e.AABBMin = aabbMin;
            e.AABBMax = aabbMax;
            e.CellIndex = cellIdx;
            e.NextInCell = _cells[cellIdx].Head;
            e.Active = true;

            _cells[cellIdx].Head = idx;
            _cells[cellIdx].Count++;
            _hostToEntry[hostId] = idx;
        }

        public void Unregister(ushort hostId)
        {
            if (!_hostToEntry.TryGetValue(hostId, out int idx)) return;
            ref var e = ref _entries[idx];
            RemoveFromCell(e.CellIndex, idx);
            e.Active = false;
            _hostToEntry.Remove(hostId);
        }

        public void Move(ushort hostId, Vector3 aabbMin, Vector3 aabbMax)
        {
            if (!_hostToEntry.TryGetValue(hostId, out int idx)) return;
            ref var e = ref _entries[idx];
            e.AABBMin = aabbMin;
            e.AABBMax = aabbMax;
            Vector3 center = (aabbMin + aabbMax) * 0.5f;
            int newCell = WorldToCellIndex(center);
            if (newCell == e.CellIndex) return;
            RemoveFromCell(e.CellIndex, idx);
            e.CellIndex = newCell;
            e.NextInCell = _cells[newCell].Head;
            _cells[newCell].Head = idx;
            _cells[newCell].Count++;
        }

        public void QueryRadius(Vector3 center, float radius,
                                SpatialQueryFilter filter, List<ushort> results)
        {
            results.Clear();
            if (radius < 0f) return;

            Vector3 min = center - Vector3.one * radius;
            Vector3 max = center + Vector3.one * radius;

            int minX = Mathf.FloorToInt(min.x / _cellSize);
            int maxX = Mathf.FloorToInt(max.x / _cellSize);
            int minY = Mathf.FloorToInt(min.y / _cellSize);
            int maxY = Mathf.FloorToInt(max.y / _cellSize);
            int minZ = Mathf.FloorToInt(min.z / _cellSize);
            int maxZ = Mathf.FloorToInt(max.z / _cellSize);

            for (int x = minX; x <= maxX; x++)
            {
                int cx = Wrap(x, _gridWidth);
                for (int y = minY; y <= maxY; y++)
                {
                    int cy = Wrap(y, _gridHeight);
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        int cz = Wrap(z, _gridDepth);
                        int cellIdx = cx + cy * _gridWidth + cz * _gridWidth * _gridHeight;
                        int entryIdx = _cells[cellIdx].Head;
                        while (entryIdx >= 0)
                        {
                            ref var e = ref _entries[entryIdx];
                            if (e.Active && Matches(e, filter)
                                && BoundsIntersectsSphere(e, center, radius))
                            {
                                AddUnique(results, e.HostId);
                            }
                            entryIdx = e.NextInCell;
                        }
                    }
                }
            }
        }

        public void Compact()
        {
            int write = 0;
            for (int read = 0; read < _entriesCount; read++)
            {
                ref var e = ref _entries[read];
                if (!e.Active) continue;
                if (write != read)
                {
                    _entries[write] = e;
                    _hostToEntry[e.HostId] = write;
                }
                write++;
            }
            for (int i = write; i < _entriesCount; i++) _entries[i] = default;
            _entriesCount = write;

            for (int i = 0; i < _cellCount; i++)
            {
                _cells[i].Head = -1;
                _cells[i].Count = 0;
            }
            for (int i = 0; i < _entriesCount; i++)
            {
                ref var e = ref _entries[i];
                e.NextInCell = _cells[e.CellIndex].Head;
                _cells[e.CellIndex].Head = i;
                _cells[e.CellIndex].Count++;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _cellCount; i++)
            {
                _cells[i].Head = -1;
                _cells[i].Count = 0;
            }
            System.Array.Clear(_entries, 0, _entriesCount);
            _entriesCount = 0;
            _hostToEntry.Clear();
        }

        private void RemoveFromCell(int cellIdx, int entryIdx)
        {
            ref var cell = ref _cells[cellIdx];
            if (cell.Head == entryIdx)
            {
                cell.Head = _entries[entryIdx].NextInCell;
                cell.Count--;
                return;
            }
            int prev = cell.Head;
            while (prev >= 0)
            {
                ref var p = ref _entries[prev];
                if (p.NextInCell == entryIdx)
                {
                    p.NextInCell = _entries[entryIdx].NextInCell;
                    cell.Count--;
                    return;
                }
                prev = p.NextInCell;
            }
        }

        private int WorldToCellIndex(Vector3 p)
        {
            int x = Wrap(Mathf.FloorToInt(p.x / _cellSize), _gridWidth);
            int y = Wrap(Mathf.FloorToInt(p.y / _cellSize), _gridHeight);
            int z = Wrap(Mathf.FloorToInt(p.z / _cellSize), _gridDepth);
            return x + y * _gridWidth + z * _gridWidth * _gridHeight;
        }

        private static int Wrap(int v, int d)
        {
            int m = v % d;
            return m < 0 ? m + d : m;
        }

        private static bool Matches(in Entry e, in SpatialQueryFilter f)
        {
            if (f.FilterByType && e.ObjectType != f.ObjectType) return false;
            if (f.FilterByOwner && e.HostId != f.OwnerId) return false;
            return true;
        }

        private static bool BoundsIntersectsSphere(in Entry e, Vector3 c, float r)
        {
            float x = Mathf.Clamp(c.x, e.AABBMin.x, e.AABBMax.x);
            float y = Mathf.Clamp(c.y, e.AABBMin.y, e.AABBMax.y);
            float z = Mathf.Clamp(c.z, e.AABBMin.z, e.AABBMax.z);
            float dx = c.x - x, dy = c.y - y, dz = c.z - z;
            return dx * dx + dy * dy + dz * dz <= r * r;
        }

        private static void AddUnique(List<ushort> list, ushort id)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == id) return;
            list.Add(id);
        }
    }
}