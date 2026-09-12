using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DynamicComponent
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct SpatialProxy
    {
        public ushort ShapeId;        // ID примитива
        public ushort OwnerId;        // ID логического объекта (Host.Id)
        public EObjectType ObjectType;// Семантический слой (ZoneTrigger, CombatCover и т.д.)
        public GeometryHandle Geometry; // Указатель на точную форму в массиве
        public Vector3 AABBMin;       // Границы Broadphase
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
        public EGeometryType Type;    // Сфера, Бокс или Треугольник
        public int Index;             // Индекс в соответствующем массиве датасета

        public GeometryHandle(EGeometryType type, int index)
        {
            Type = type;
            Index = index;
        }
        public bool IsValid => Index >= 0;
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
    public struct TriangleGeometry
    {
        public Vector3 A;
        public Vector3 B;
        public Vector3 C;
    }
}
