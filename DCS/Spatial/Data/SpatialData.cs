using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DCS.Spatial
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct SpatialProxy
    {
        public ushort ShapeId;
        public ushort OwnerId;        // ID логического хоста
        public EObjectType ObjectType;
        public GeometryHandle Geometry;
        public Vector3 AABBMin;       // Границы для быстрого поиска (Broadphase)
        public Vector3 AABBMax;

        public SpatialProxy(ushort shapeId, ushort ownerId, EObjectType objectType, GeometryHandle geometry, Vector3 aabbMin, Vector3 aabbMax)
        {
            ShapeId = shapeId;
            OwnerId = ownerId;
            ObjectType = objectType;
            Geometry = geometry;
            AABBMin = aabbMin;
            AABBMax = aabbMax;
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct GeometryHandle
    {
        public EGeometryType Type;
        public int Index;

        public GeometryHandle(EGeometryType type, int index)
        {
            Type = type;
            Index = index;
        }
        public bool IsValid => Index >= 0;
    }

    [Serializable]
    public struct SphereGeometry { public Vector3 Center; public float Radius; }

    [Serializable]
    public struct BoxGeometry { public Vector3 Center; public Vector3 Extents; public Quaternion Rotation; }

    [Serializable]
    public struct TriangleGeometry { public Vector3 A; public Vector3 B; public Vector3 C; }
}
