using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DCS.Spatial
{
    public enum ESpatialObjectType : byte
    {
        Generic = 0,
        Region = 1,
        Trigger = 2,
        StrongPoint = 3
    }

    public enum EGeometryType : byte
    {
        Sphere = 0,
        Box = 1,
        Cylinder = 2,
        Polygon = 3
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
        public static GeometryHandle Invalid => new GeometryHandle(EGeometryType.Sphere, -1);
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct SpatialProxy
    {
        public ushort SpatialId;
        public ushort OwnerId;
        public ESpatialObjectType ObjectType;
        public GeometryHandle Geometry;
        public Vector3 AABBMin;
        public Vector3 AABBMax;

        public SpatialProxy(
            ushort spatialId,
            ushort ownerId,
            ESpatialObjectType objectType,
            GeometryHandle geometry,
            Vector3 aabbMin,
            Vector3 aabbMax)
        {
            SpatialId = spatialId;
            OwnerId = ownerId;
            ObjectType = objectType;
            Geometry = geometry;
            AABBMin = aabbMin;
            AABBMax = aabbMax;
        }
    }

    [Serializable]
    public struct SphereGeometry
    {
        public Vector3 Center;
        public float Radius;
    }

    [Serializable]
    public struct BoxGeometry
    {
        public Vector3 Center;
        public Vector3 Extents;
        public Quaternion Rotation;
    }

    [Serializable]
    public struct CylinderGeometry
    {
        public Vector3 Center;
        public float Radius;
        public float HalfHeight;
        public Quaternion Rotation;
    }

    [Serializable]
    public struct PolygonGeometry
    {
        public int StartIndex;
        public int PointCount;
        public float MinY;
        public float MaxY;
        public bool Closed;
    }
}
