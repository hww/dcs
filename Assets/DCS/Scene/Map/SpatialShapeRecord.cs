using System.Runtime.InteropServices;
using UnityEngine;

namespace DynamicComponent
{
    public enum EObjectType : byte
    {
        ZoneTrigger,      // Игровые зоны, регионы
        CombatCover,      // Места для укрытий (OBB)
        GrapplePoint,     // Точки зацепа, крюки (Сферы)
        InteractableNode  // Лестницы, проемы для прыжков
    }

    public enum EGeometryType : byte
    {
        Sphere,
        Box,
        Triangle
    }

    /// <summary>
    /// Универсальный spatial-прокси. Именно его видит пространственная сетка.
    /// Он полностью Blittable (каждая структура занимает фиксированное место в памяти).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SpatialShapeRecord
    {
        public ushort ShapeId;         // Уникальный ID самого примитива
        public ushort OwnerId;         // ID логического владельца (Host.Id зоны или укрытия)
        public EObjectType ObjectType; // Семантика: что это такое?
        public EGeometryType GeomType; // Математика: как это просчитывать?

        // --- Выровненные данные Broadphase (Мировой AABB) ---
        // Больше никакого округления через гигантский maxExtent! Считаем честный Bounds.
        public Vector3 AABBMin;
        public Vector3 AABBMax;

        // --- Универсальный геометрический контейнер (Narrowphase) ---
        public Vector3 RawData0;       // Центр (Sphere/Box) ИЛИ Вершина 0 (Triangle)
        public Vector3 RawData1;       // Extents (Box)      ИЛИ Вершина 1 (Triangle) ИЛИ X как Радиус (Sphere)
        public Vector3 RawData2;       // Не используется     ИЛИ Вершина 2 (Triangle)
        public Quaternion Rotation;    // Поворот (нужен только для OBB Box)
    }
}
