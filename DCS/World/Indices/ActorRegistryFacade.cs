using DCS.Spatial;
using System.Collections.Generic;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Thin facade over HostManager + NameIndex + TagIndex + DynamicSpatial.
    /// Replaces the old ActorRegistry. Does NOT own identity — Host owns identity.
    /// </summary>
    public static class ActorRegistryFacade
    {
        public static NameIndex Names { get; private set; }
        public static TagIndex Tags { get; private set; }
        public static Spatial.DynamicSpatial Spatial { get; private set; }

        public static void Initialize(NameIndex names, TagIndex tags, Spatial.DynamicSpatial spatial)
        {
            Names = names;
            Tags = tags;
            Spatial = spatial;
        }

        public static int Count => Names != null ? Names.Count : 0;

        public static bool TryGetByName(string name, out Host host)
        {
            host = default;
            if (Names == null || string.IsNullOrEmpty(name)) return false;
            if (!Names.TryGet(name, out ushort id)) return false;
            if (id >= HostManager.MaxGameObjects) return false;
            host = new Host { Id = id, Generation = HostManager.GlobalHosts[id].Generation };
            return HostManager.IsValid(host);
        }

        public static void GetByTag(int bit, List<ushort> results)
        {
            results.Clear();
            Tags?.GetByTag(bit, results);
        }

        public static void QueryRadius(Vector3 center, float radius, List<ushort> results)
        {
            results.Clear();
            Spatial?.QueryRadius(center, radius, SpatialQueryFilter.Any, results);
        }
    }
}