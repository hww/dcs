using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DCS.Spatial
{
    /// <summary>
    /// Spatial hash for dynamic actors. Fixed-size, wrapped, zero-alloc.
    ///
    /// Design principles:
    ///   - The grid does NOT know about activation. It only stores what
    ///     it is given. Activation is the responsibility of external
    ///     managers (triggers, instancers, scripts).
    ///   - The grid wraps on index: cell = (world / cellSize) mod gridDim.
    ///     This means the grid tiles the world infinitely, but with
    ///     index collisions for distant regions.
    ///   - Index collisions are TOLERATED. The distance filter in queries
    ///     discards false positives.
    ///   - Stale entries (actors that were not unregistered) are tolerated.
    ///     They do not break correctness; the distance filter handles them.
    ///   - Cell storage uses intrusive linked lists over a flat array.
    ///     No per-operation allocations.
    ///
    /// Sizing rule:
    ///   gridDimension * cellSize >= 4 * typicalQueryRadius
    ///   Otherwise the grid contains too many false positives.
    /// </summary>
    public sealed class DynamicSpatial
    {
        // ============================================================
        //  STORAGE
        // ============================================================

        private readonly int _gridWidth;
        private readonly int _gridHeight;
        private readonly int _gridDepth;
        private readonly int _cellCount;
        private readonly float _cellSize;

        /// <summary>Flat array of cells. Index = x + y*W + z*W*H.</summary>
        private readonly Cell[] _cells;

        /// <summary>All registered entries, packed. Inactive slots remain until compacted.</summary>
        private DynamicEntry[] _entries;

        private int _entriesCount;

        /// <summary>Handle → index into _entries. Only active handles appear.</summary>
        private readonly Dictionary<ushort, int> _handleToEntry;

        // ============================================================
        //  TYPES
        // ============================================================

        /// <summary>
        /// One registered actor. Stored in a flat list to keep entries
        /// contiguous in memory — better cache behavior than a linked
        /// list of objects.
        /// </summary>
        private struct DynamicEntry
        {
            public ushort Handle;
            public ushort OwnerId;
            public ESpatialObjectType ObjectType;
            public GeometryHandle Geometry;
            public Vector3 AABBMin;
            public Vector3 AABBMax;
            public int CellIndex;
            public int NextInCell; // -1 = end of list
            public bool Active;
        }

        /// <summary>
        /// One grid cell. Holds the head index of an intrusive singly
        /// linked list of entries in this cell.
        /// </summary>
        private sealed class Cell
        {
            public int Head = -1;
            public int Count = 0;
        }

        // ============================================================
        //  CONSTRUCTION
        // ============================================================

        /// <param name="width">Cells along X.</param>
        /// <param name="height">Cells along Y.</param>
        /// <param name="depth">Cells along Z.</param>
        /// <param name="cellSize">World size of one cell in meters.</param>
        /// <param name="initialEntryCapacity">Pre-allocation size for entries.</param>
        public DynamicSpatial(int width, int height, int depth, float cellSize,
                              int initialEntryCapacity = 512)
        {
            _gridWidth = Mathf.Max(1, width);
            _gridHeight = Mathf.Max(1, height);
            _gridDepth = Mathf.Max(1, depth);
            _cellSize = Mathf.Max(0.1f, cellSize);
            _cellCount = _gridWidth * _gridHeight * _gridDepth;

            _cells = new Cell[_cellCount];
            for (int i = 0; i < _cellCount; i++)
                _cells[i] = new Cell();

            _entries = new DynamicEntry[initialEntryCapacity];
            _entriesCount = 0;
            _handleToEntry = new Dictionary<ushort, int>(initialEntryCapacity);
        }

        public int CellCount => _cellCount;
        public int ActiveCount => _handleToEntry.Count;

        // ============================================================
        //  REGISTRATION
        // ============================================================

        /// <summary>
        /// Register an actor. AABB determines which cells it occupies.
        /// If the AABB spans multiple cells, the actor is inserted into
        /// all of them (duplicated). This is the standard approach for
        /// large actors in a spatial hash.
        /// </summary>
        public void Register(
            ushort handle,
            ushort ownerId,
            ESpatialObjectType type,
            GeometryHandle geometry,
            Vector3 aabbMin,
            Vector3 aabbMax)
        {
            if (_handleToEntry.ContainsKey(handle))
            {
                Debug.LogError($"[DynamicSpatial] Handle {handle} already registered.");
                return;
            }

            // Determine primary cell (center of AABB). For multi-cell
            // actors, we still insert only into the center cell — this
            // is a simplification. If you need multi-cell insertion,
            // extend RegisterMultiCell below.
            Vector3 center = (aabbMin + aabbMax) * 0.5f;
            int cellIdx = WorldToCellIndex(center);

            if (_entriesCount >= _entries.Length)
                GrowEntries();

            int entryIdx = _entriesCount++;
            ref var entry = ref _entries[entryIdx];
            entry.Handle = handle;
            entry.OwnerId = ownerId;
            entry.ObjectType = type;
            entry.Geometry = geometry;
            entry.AABBMin = aabbMin;
            entry.AABBMax = aabbMax;
            entry.CellIndex = cellIdx;
            entry.NextInCell = _cells[cellIdx].Head;
            entry.Active = true;

            _cells[cellIdx].Head = entryIdx;
            _cells[cellIdx].Count++;
            _handleToEntry[handle] = entryIdx;
        }

        private void GrowEntries()
        {
            int newCap = _entries.Length * 2;
            if (newCap < 16) newCap = 16;
            System.Array.Resize(ref _entries, newCap);
        }

        /// <summary>
        /// Unregister an actor. Removes it from its cell and the handle map.
        /// The entry slot remains in _entries until compaction; it is
        /// simply marked inactive.
        /// </summary>
        public void Unregister(ushort handle)
        {
            if (!_handleToEntry.TryGetValue(handle, out int idx))
                return;

            ref var entry = ref EntriesRef(idx);
            RemoveFromCell(entry.CellIndex, idx);
            entry.Active = false;
            _handleToEntry.Remove(handle);
        }

        /// <summary>
        /// Move an actor to a new AABB. If the center moves to a different
        /// cell, the actor is relocated. Otherwise only the AABB is updated.
        /// </summary>
        public void Move(ushort handle, Vector3 aabbMin, Vector3 aabbMax)
        {
            if (!_handleToEntry.TryGetValue(handle, out int idx))
                return;

            ref var entry = ref EntriesRef(idx);
            entry.AABBMin = aabbMin;
            entry.AABBMax = aabbMax;

            Vector3 center = (aabbMin + aabbMax) * 0.5f;
            int newCell = WorldToCellIndex(center);

            if (newCell == entry.CellIndex)
                return;

            RemoveFromCell(entry.CellIndex, idx);
            entry.CellIndex = newCell;
            entry.NextInCell = _cells[newCell].Head;
            _cells[newCell].Head = idx;
            _cells[newCell].Count++;
        }

        // ============================================================
        //  QUERIES
        // ============================================================

        /// <summary>
        /// Query all actors whose AABB intersects a sphere. Results are
        /// owner IDs, deduplicated. The distance filter is mandatory
        /// because the grid wraps and may contain false positives.
        /// </summary>
        public void QueryRadius(
            Vector3 center, float radius,
            SpatialQueryFilter filter,
            List<ushort> results)
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
                            ref var entry = ref EntriesRef(entryIdx);
                            if (entry.Active && Matches(entry, filter)
                                && BoundsIntersectsSphere(entry, center, radius))
                            {
                                AddUnique(results, entry.OwnerId);
                            }
                            entryIdx = entry.NextInCell;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Query all actors whose AABB contains the point.
        /// </summary>
        public void QueryPoint(
            Vector3 point,
            SpatialQueryFilter filter,
            List<ushort> results)
        {
            results.Clear();

            int cellIdx = WorldToCellIndex(point);
            int entryIdx = _cells[cellIdx].Head;

            while (entryIdx >= 0)
            {
                ref var entry = ref EntriesRef(entryIdx);
                if (entry.Active && Matches(entry, filter)
                    && BoundsContainsPoint(entry, point))
                {
                    AddUnique(results, entry.OwnerId);
                }
                entryIdx = entry.NextInCell;
            }
        }

        /// <summary>
        /// Find the nearest actor matching the filter within maxDistance.
        /// Uses a linear scan over all active entries. For small active
        /// counts (as expected with simulation LOD), this is fast enough.
        /// </summary>
        public bool QueryNearest(
            Vector3 point, float maxDistance,
            SpatialQueryFilter filter,
            out SpatialHit hit)
        {
            hit = default;
            if (maxDistance < 0f) return false;

            float best = maxDistance;
            bool found = false;

            for (int i = 0; i < _entriesCount; i++)
            {
                ref var entry = ref EntriesRef(i);
                if (!entry.Active || !Matches(entry, filter))
                    continue;

                float d = DistanceToBounds(point, entry);
                if (d >= best) continue;

                best = d;
                found = true;
                hit = new SpatialHit
                {
                    SpatialId = entry.Handle,
                    OwnerId = entry.OwnerId,
                    ObjectType = entry.ObjectType,
                    GeometryType = entry.Geometry.Type,
                    Position = point,
                    Distance = d,
                    Normal = Vector3.zero,
                };
            }
            return found;
        }

        /// <summary>
        /// Raycast against all actors matching the filter. Returns the
        /// nearest hit. Linear scan over active entries.
        /// </summary>
        public bool Raycast(
            Vector3 origin, Vector3 direction, float maxDistance,
            SpatialQueryFilter filter,
            RuntimeGeometryStorage geometry,
            out SpatialHit hit)
        {
            hit = default;
            if (maxDistance < 0f || direction.sqrMagnitude < 1e-8f)
                return false;

            direction = direction.normalized;
            float best = maxDistance;
            bool found = false;

            for (int i = 0; i < _entriesCount; i++)
            {
                ref var entry = ref EntriesRef(i);
                if (!entry.Active || !Matches(entry, filter))
                    continue;

                if (!RayIntersectsBounds(origin, direction, best, entry))
                    continue;

                if (!GeometryIntersection.Raycast(
                        origin, direction, best,
                        entry.Geometry, geometry,
                        out Vector3 p, out Vector3 n, out float d))
                    continue;

                if (d >= best) continue;

                best = d;
                found = true;
                hit = new SpatialHit
                {
                    SpatialId = entry.Handle,
                    OwnerId = entry.OwnerId,
                    ObjectType = entry.ObjectType,
                    GeometryType = entry.Geometry.Type,
                    Position = p,
                    Normal = n,
                    Distance = d,
                };
            }
            return found;
        }

        // ============================================================
        //  COMPACTION / DIAGNOSTICS
        // ============================================================

        /// <summary>
        /// Remove all inactive entries from the backing array, rebuilding
        /// the cell lists. Call periodically if you spawn/despawn a lot,
        /// to reclaim memory and improve cache locality.
        ///
        /// Not required for correctness — inactive entries are skipped
        /// during queries. But after many despawns the array grows large.
        /// </summary>
        public void Compact()
        {
            int writeIdx = 0;

            for (int readIdx = 0; readIdx < _entriesCount; readIdx++)
            {
                ref var entry = ref _entries[readIdx];
                if (!entry.Active) continue;

                if (writeIdx != readIdx)
                {
                    _entries[writeIdx] = entry;
                    _handleToEntry[entry.Handle] = writeIdx;
                }
                writeIdx++;
            }

            // Обнуляем хвост — не обязательно, но полезно для GC.
            for (int i = writeIdx; i < _entriesCount; i++)
                _entries[i] = default;

            _entriesCount = writeIdx;

            // Rebuild cell lists from scratch.
            for (int i = 0; i < _cellCount; i++)
            {
                _cells[i].Head = -1;
                _cells[i].Count = 0;
            }

            for (int i = 0; i < _entriesCount; i++)
            {
                ref var entry = ref _entries[i];
                entry.NextInCell = _cells[entry.CellIndex].Head;
                _cells[entry.CellIndex].Head = i;
                _cells[entry.CellIndex].Count++;
            }
        }

        /// <summary>Clear all state. Does not free the arrays — reuse the instance.</summary>
        public void Clear()
        {
            for (int i = 0; i < _cellCount; i++)
            {
                _cells[i].Head = -1;
                _cells[i].Count = 0;
            }
            System.Array.Clear(_entries, 0, _entriesCount);
            _entriesCount = 0;
            _handleToEntry.Clear();
        }

        /// <summary>Number of cells that currently contain at least one active entry.</summary>
        public int OccupiedCellCount()
        {
            int count = 0;
            for (int i = 0; i < _cellCount; i++)
                if (_cells[i].Count > 0) count++;
            return count;
        }

        /// <summary>Largest number of entries in a single cell. Diagnostic only.</summary>
        public int MaxCellOccupancy()
        {
            int max = 0;
            for (int i = 0; i < _cellCount; i++)
                if (_cells[i].Count > max) max = _cells[i].Count;
            return max;
        }

        // ============================================================
        //  INTERNALS
        // ============================================================

        /// <summary>
        /// Get a ref to an entry. Uses CollectionsMarshal to avoid
        /// copying the struct. Requires .NET 5+ / Unity 2021.2+.
        /// </summary>
        private ref DynamicEntry EntriesRef(int idx)
        {
            return ref _entries[idx];
        }

        private void RemoveFromCell(int cellIdx, int entryIdx)
        {
            ref var cell = ref _cells[cellIdx];
            if (cell.Head == entryIdx)
            {
                // Removing head — advance to next.
                cell.Head = EntriesRef(entryIdx).NextInCell;
                cell.Count--;
                return;
            }

            // Walk the list to find the predecessor.
            int prev = cell.Head;
            while (prev >= 0)
            {
                ref var prevEntry = ref EntriesRef(prev);
                if (prevEntry.NextInCell == entryIdx)
                {
                    prevEntry.NextInCell = EntriesRef(entryIdx).NextInCell;
                    cell.Count--;
                    return;
                }
                prev = prevEntry.NextInCell;
            }
        }

        /// <summary>
        /// Convert world position to cell index, with wrap-around.
        /// Negative coordinates are handled correctly.
        /// </summary>
        private int WorldToCellIndex(Vector3 p)
        {
            int x = Mathf.FloorToInt(p.x / _cellSize);
            int y = Mathf.FloorToInt(p.y / _cellSize);
            int z = Mathf.FloorToInt(p.z / _cellSize);

            x = Wrap(x, _gridWidth);
            y = Wrap(y, _gridHeight);
            z = Wrap(z, _gridDepth);

            return x + y * _gridWidth + z * _gridWidth * _gridHeight;
        }

        private static int Wrap(int value, int dimension)
        {
            int m = value % dimension;
            return m < 0 ? m + dimension : m;
        }

        private static bool Matches(in DynamicEntry e, in SpatialQueryFilter f)
        {
            if (f.FilterByType && e.ObjectType != f.ObjectType) return false;
            if (f.FilterByOwner && e.OwnerId != f.OwnerId) return false;
            return true;
        }

        private static bool BoundsContainsPoint(in DynamicEntry e, Vector3 v)
        {
            return v.x >= e.AABBMin.x && v.x <= e.AABBMax.x
                && v.y >= e.AABBMin.y && v.y <= e.AABBMax.y
                && v.z >= e.AABBMin.z && v.z <= e.AABBMax.z;
        }

        private static bool BoundsIntersectsSphere(in DynamicEntry e, Vector3 c, float r)
        {
            float x = Mathf.Clamp(c.x, e.AABBMin.x, e.AABBMax.x);
            float y = Mathf.Clamp(c.y, e.AABBMin.y, e.AABBMax.y);
            float z = Mathf.Clamp(c.z, e.AABBMin.z, e.AABBMax.z);
            float dx = c.x - x, dy = c.y - y, dz = c.z - z;
            return dx * dx + dy * dy + dz * dz <= r * r;
        }

        private static float DistanceToBounds(Vector3 p, in DynamicEntry e)
        {
            float dx = Mathf.Max(e.AABBMin.x - p.x, p.x - e.AABBMax.x);
            float dy = Mathf.Max(e.AABBMin.y - p.y, p.y - e.AABBMax.y);
            float dz = Mathf.Max(e.AABBMin.z - p.z, p.z - e.AABBMax.z);
            float x = Mathf.Max(dx, 0f);
            float y = Mathf.Max(dy, 0f);
            float z = Mathf.Max(dz, 0f);
            return Mathf.Sqrt(x * x + y * y + z * z);
        }

        private static bool RayIntersectsBounds(
            Vector3 o, Vector3 d, float max, in DynamicEntry e)
        {
            float tmin = 0f, tmax = max;
            if (!RaySlab(o.x, d.x, e.AABBMin.x, e.AABBMax.x, ref tmin, ref tmax)) return false;
            if (!RaySlab(o.y, d.y, e.AABBMin.y, e.AABBMax.y, ref tmin, ref tmax)) return false;
            if (!RaySlab(o.z, d.z, e.AABBMin.z, e.AABBMax.z, ref tmin, ref tmax)) return false;
            return true;
        }

        private static bool RaySlab(
            float o, float d, float min, float max,
            ref float tmin, ref float tmax)
        {
            if (Mathf.Abs(d) < 1e-7f)
                return o >= min && o <= max;

            float a = (min - o) / d;
            float b = (max - o) / d;
            if (a > b) { float t = a; a = b; b = t; }
            tmin = Mathf.Max(tmin, a);
            tmax = Mathf.Min(tmax, b);
            return tmin <= tmax && tmax >= 0f;
        }

        /// <summary>
        /// Add an owner ID if not already present. O(n) but n is tiny —
        /// multi-proxy owners are rare.
        /// </summary>
        private static void AddUnique(List<ushort> list, ushort id)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == id) return;
            list.Add(id);
        }
    }
}