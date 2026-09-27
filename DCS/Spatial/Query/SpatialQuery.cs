using DCS.Core;
using System.Collections.Generic;
using UnityEngine;

namespace DCS.Spatial
{
    public static class SpatialQuery
    {
        public static void Point(int domainId, Vector3 point, SpatialQueryFilter filter, List<ushort> owners)
        {
            var spatial = Resolve(domainId);
            spatial?.QueryPoint(point, filter, owners);
        }

        public static void Radius(int domainId, Vector3 center, float radius, SpatialQueryFilter filter, List<ushort> owners)
        {
            var spatial = Resolve(domainId);
            spatial?.QueryRadius(center, radius, filter, owners);
        }

        public static bool Nearest(int domainId, Vector3 point, float maxDistance, SpatialQueryFilter filter, out SpatialHit hit)
        {
            hit = default;
            var spatial = Resolve(domainId);
            return spatial != null && spatial.QueryNearest(point, maxDistance, filter, out hit);
        }

        public static bool Raycast(int domainId, Vector3 origin, Vector3 direction, float maxDistance, SpatialQueryFilter filter, out SpatialHit hit)
        {
            hit = default;
            var spatial = Resolve(domainId);
            return spatial != null && spatial.Raycast(origin, direction, maxDistance, filter, out hit);
        }

        public static bool Contains(int domainId, Vector3 point, ushort ownerId, ESpatialObjectType type)
        {
            var spatial = Resolve(domainId);
            return spatial != null && spatial.Contains(point, ownerId, type);
        }

        private static SpatialRuntime Resolve(int domainId)
        {
            if (!SpatialDomainRegistry.TryGet(domainId, out var sd)) return null;
            return sd.StaticSpatial;
        }
    }
}