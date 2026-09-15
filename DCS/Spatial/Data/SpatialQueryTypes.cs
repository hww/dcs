using System;
using UnityEngine;

namespace DCS.Spatial
{
    [Serializable]
    public struct SpatialQueryFilter
    {
        public bool FilterByType;
        public ESpatialObjectType ObjectType;
        public bool FilterByOwner;
        public ushort OwnerId;
        public static SpatialQueryFilter Any => default;
        public static SpatialQueryFilter ByType(ESpatialObjectType type) => new SpatialQueryFilter { FilterByType = true, ObjectType = type };
        public static SpatialQueryFilter ByOwner(ushort ownerId) => new SpatialQueryFilter { FilterByOwner = true, OwnerId = ownerId };
        public static SpatialQueryFilter ByTypeAndOwner(ESpatialObjectType type, ushort ownerId) => new SpatialQueryFilter { FilterByType = true, ObjectType = type, FilterByOwner = true, OwnerId = ownerId };
    }

    [Serializable]
    public struct SpatialHit
    {
        public ushort SpatialId;
        public ushort OwnerId;
        public ESpatialObjectType ObjectType;
        public EGeometryType GeometryType;
        public Vector3 Position;
        public Vector3 Normal;
        public float Distance;
    }
}
