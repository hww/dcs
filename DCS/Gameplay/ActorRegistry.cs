using DCS.Spatial;
using System.Collections.Generic;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Single global registry of all actors in the game world.
    /// Provides semantic search by name, type, and tag.
    /// Does NOT depend on scene hierarchy, scene count, or spatial layout.
    ///
    /// Lifecycle:
    ///   - Static scene actors: registered in batch during scene load.
    ///   - Dynamic actors: registered on spawn, unregistered on despawn.
    ///   - Activation/deactivation is a separate concern (see SpatialActivator)
    ///     and does NOT change registration.
    /// </summary>
    public sealed class ActorRegistry
    {
        // ============================================================
        //  STORAGE
        // ============================================================

        /// <summary>All records, indexed by handle. Sparse — entries may be inactive.</summary>
        private readonly Dictionary<ushort, ActorRecord> _records;

        /// <summary>Name → handle. Only one actor per name (uniqueness contract).</summary>
        private readonly Dictionary<string, ushort> _byName;

        /// <summary>Type → list of handles. Handles may become stale on unregister.</summary>
        private readonly Dictionary<ESpatialObjectType, List<ushort>> _byType;

        /// <summary>Tag → list of handles.</summary>
        private readonly Dictionary<string, List<ushort>> _byTag;

        /// <summary>Scene → list of handles. Used for batch unregister on scene unload.</summary>
        private readonly Dictionary<ushort, List<ushort>> _byScene;

        /// <summary>Pool of free handles for reuse.</summary>
        private readonly Stack<ushort> _freeHandles;

        /// <summary>Next handle to allocate if free pool is empty.</summary>
        private ushort _nextHandle;

        // ============================================================
        //  CONSTRUCTION
        // ============================================================

        public ActorRegistry(int initialCapacity = 1024)
        {
            _records = new Dictionary<ushort, ActorRecord>(initialCapacity);
            _byName = new Dictionary<string, ushort>(initialCapacity);
            _byType = new Dictionary<ESpatialObjectType, List<ushort>>();
            _byTag = new Dictionary<string, List<ushort>>();
            _byScene = new Dictionary<ushort, List<ushort>>();
            _freeHandles = new Stack<ushort>();
            _nextHandle = 1; // 0 reserved as null handle

            // Pre-allocate type buckets for all enum values
            foreach (ESpatialObjectType t in System.Enum.GetValues(typeof(ESpatialObjectType)))
            {
                _byType[t] = new List<ushort>(64);
            }
        }

        // ============================================================
        //  REGISTRATION
        // ============================================================

        /// <summary>
        /// Register an actor. Returns false if the name is already taken.
        /// This is a hard contract — the caller must ensure unique names,
        /// or explicitly pass null/empty to skip name registration.
        /// </summary>
        public bool Register(
            ushort handle,
            string name,
            ESpatialObjectType type,
            string[] tags,
            ushort ownerId,
            Vector3 position,
            ushort sceneId)
        {
            // Reject duplicate handles — this is a programming error.
            if (_records.ContainsKey(handle))
            {
                Debug.LogError($"[ActorRegistry] Handle {handle} already registered.");
                return false;
            }

            // Reject duplicate names — this is a content error.
            if (!string.IsNullOrEmpty(name) && _byName.ContainsKey(name))
            {
                Debug.LogError($"[ActorRegistry] Name '{name}' already registered.");
                return false;
            }

            var record = new ActorRecord
            {
                Handle = handle,
                Name = name,
                ObjectType = type,
                Tags = tags ?? System.Array.Empty<string>(),
                OwnerId = ownerId,
                Position = position,
                SceneId = sceneId,
                InSpatial = false,
                Active = false,
            };

            _records[handle] = record;

            if (!string.IsNullOrEmpty(name))
                _byName[name] = handle;

            _byType[type].Add(handle);

            foreach (var tag in record.Tags)
            {
                if (!_byTag.TryGetValue(tag, out var list))
                {
                    list = new List<ushort>(32);
                    _byTag[tag] = list;
                }
                list.Add(handle);
            }

            if (!_byScene.TryGetValue(sceneId, out var sceneList))
            {
                sceneList = new List<ushort>(128);
                _byScene[sceneId] = sceneList;
            }
            sceneList.Add(handle);

            return true;
        }

        /// <summary>
        /// Unregister an actor by handle. Removes it from all indices.
        /// Safe to call multiple times — subsequent calls are no-ops.
        /// </summary>
        public void Unregister(ushort handle)
        {
            if (!_records.TryGetValue(handle, out var record))
                return;

            if (!string.IsNullOrEmpty(record.Name))
                _byName.Remove(record.Name);

            RemoveFromList(_byType[record.ObjectType], handle);

            foreach (var tag in record.Tags)
            {
                if (_byTag.TryGetValue(tag, out var list))
                    RemoveFromList(list, handle);
            }

            if (_byScene.TryGetValue(record.SceneId, out var sceneList))
                RemoveFromList(sceneList, handle);

            _records.Remove(handle);
            _freeHandles.Push(handle);
        }

        /// <summary>Unregister all actors belonging to a scene. Used on scene unload.</summary>
        public void UnregisterScene(ushort sceneId)
        {
            if (!_byScene.TryGetValue(sceneId, out var handles))
                return;

            // Copy to avoid mutation during iteration.
            var copy = handles.ToArray();
            foreach (var h in copy)
                Unregister(h);

            _byScene.Remove(sceneId);
        }

        // ============================================================
        //  QUERIES
        // ============================================================

        public bool TryGetByName(string name, out ushort handle)
            => _byName.TryGetValue(name, out handle);

        public bool TryGetRecord(ushort handle, out ActorRecord record)
            => _records.TryGetValue(handle, out record);

        /// <summary>Collect all handles of a given type. Results are cleared first.</summary>
        public void GetByType(ESpatialObjectType type, List<ushort> results)
        {
            results.Clear();
            if (_byType.TryGetValue(type, out var list))
                results.AddRange(list);
        }

        /// <summary>Collect all handles with a given tag. Results are cleared first.</summary>
        public void GetByTag(string tag, List<ushort> results)
        {
            results.Clear();
            if (_byTag.TryGetValue(tag, out var list))
                results.AddRange(list);
        }

        /// <summary>
        /// Combined filter: name AND/OR type AND/OR tag.
        /// Null/empty parameters are ignored.
        /// Used by the Lua FindSceneObjects API.
        /// </summary>
        public void Find(
            string name,
            ESpatialObjectType? type,
            string tag,
            List<ushort> results)
        {
            results.Clear();

            // Fast path: exact name lookup
            if (!string.IsNullOrEmpty(name))
            {
                if (_byName.TryGetValue(name, out ushort handle))
                {
                    if (!_records.TryGetValue(handle, out var rec))
                        return;

                    if (type.HasValue && rec.ObjectType != type.Value)
                        return;

                    if (!string.IsNullOrEmpty(tag) && !HasTag(rec.Tags, tag))
                        return;

                    results.Add(handle);
                }
                return;
            }

            // Name is null — iterate the smallest bucket available.
            List<ushort> source = null;

            if (type.HasValue)
                source = _byType[type.Value];
            else if (!string.IsNullOrEmpty(tag))
                source = _byTag.TryGetValue(tag, out var tagList) ? tagList : null;

            if (source == null)
                return;

            foreach (var h in source)
            {
                if (!_records.TryGetValue(h, out var rec))
                    continue;

                if (type.HasValue && rec.ObjectType != type.Value)
                    continue;

                if (!string.IsNullOrEmpty(tag) && !HasTag(rec.Tags, tag))
                    continue;

                results.Add(h);
            }
        }

        public int Count => _records.Count;

        // ============================================================
        //  HELPERS
        // ============================================================

        /// <summary>Allocate a fresh handle. Reuses freed handles when possible.</summary>
        public ushort AllocateHandle()
        {
            if (_freeHandles.Count > 0)
                return _freeHandles.Pop();

            ushort h = _nextHandle++;
            if (h == 0)
            {
                Debug.LogError("[ActorRegistry] Handle space exhausted.");
                return 0;
            }
            return h;
        }

        /// <summary>Update the cached position of a record. Called on Move.</summary>
        public void UpdatePosition(ushort handle, Vector3 position)
        {
            if (_records.TryGetValue(handle, out var rec))
            {
                rec.Position = position;
                _records[handle] = rec;
            }
        }

        private static void RemoveFromList(List<ushort> list, ushort handle)
        {
            // O(n) removal. Acceptable because lists are small and
            // unregister is not a hot path.
            int idx = list.IndexOf(handle);
            if (idx < 0) return;

            int last = list.Count - 1;
            list[idx] = list[last];
            list.RemoveAt(last);
        }

        private static bool HasTag(string[] tags, string tag)
        {
            for (int i = 0; i < tags.Length; i++)
                if (tags[i] == tag) return true;
            return false;
        }
    }
}