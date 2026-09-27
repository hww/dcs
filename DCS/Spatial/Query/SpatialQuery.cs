using System.Collections.Generic;
using UnityEngine;

namespace DCS.Spatial
{
    public static class SpatialQuery
    {
        public static void Point(Vector3 point, SpatialQueryFilter filter, List<ushort> owners) => SpatialRuntime.Instance?.QueryPoint(point, filter, owners);
        public static void Radius(Vector3 center, float radius, SpatialQueryFilter filter, List<ushort> owners) => SpatialRuntime.Instance?.QueryRadius(center, radius, filter, owners);
        public static bool Nearest(Vector3 point, float maxDistance, SpatialQueryFilter filter, out SpatialHit hit) { hit=default; return SpatialRuntime.Instance != null && SpatialRuntime.Instance.QueryNearest(point,maxDistance,filter,out hit); }
        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, SpatialQueryFilter filter, out SpatialHit hit) { hit=default; return SpatialRuntime.Instance != null && SpatialRuntime.Instance.Raycast(origin,direction,maxDistance,filter,out hit); }
        public static bool Contains(Vector3 point, ushort ownerId, ESpatialObjectType type) => SpatialRuntime.Instance != null && SpatialRuntime.Instance.Contains(point, ownerId, type);
    }
}
