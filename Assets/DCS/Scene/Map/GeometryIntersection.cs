using UnityEngine;

namespace DynamicComponent
{
    public static class GeometryIntersection
    {
        /// <summary>
        /// Универсальный шлюз узкой фазы (Narrowphase)
        /// </summary>
        public static bool IsPointInside(Vector3 point, ref SpatialShapeRecord prim)
        {
            switch (prim.GeomType)
            {
                case EGeometryType.Sphere:
                    // Проверка сферы: квадрат расстояния (0 тригонометрии, ультра-быстро)
                    float sqrDistance = (prim.RawData0 - point).sqrMagnitude;
                    float sqrRadius = prim.RawData1.x * prim.RawData1.x; // Радиус запечен в RawData1.x
                    return sqrDistance <= sqrRadius;

                case EGeometryType.Box:
                    // Проверка повернутого куба (OBB) через локальное пространство
                    // RawData0 - это центр, RawData1 - полуразмеры (Extents)
                    Vector3 localPoint = Quaternion.Inverse(prim.Rotation) * (point - prim.RawData0);
                    return Mathf.Abs(localPoint.x) <= prim.RawData1.x &&
                           Mathf.Abs(localPoint.y) <= prim.RawData1.y &&
                           Mathf.Abs(localPoint.z) <= prim.RawData1.z;

                case EGeometryType.Triangle:
                    // Проверка попадания в плоский треугольник (проекция на плоскость XZ для 2D-зон)
                    // RawData0, RawData1, RawData2 — это вершины треугольника в мировых координатах
                    return IsPointInTriangle2D(point, prim.RawData0, prim.RawData1, prim.RawData2);
            }
            return false;
        }

        private static bool IsPointInTriangle2D(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            float s1 = (a.x - p.x) * (b.z - p.z) - (b.x - a.x) * (a.z - p.z);
            float s2 = (b.x - p.x) * (c.z - p.z) - (c.x - b.x) * (b.z - p.z);
            float s3 = (c.x - p.x) * (a.z - p.z) - (a.x - c.x) * (c.z - p.z);
            return (s1 >= 0 && s2 >= 0 && s3 >= 0) || (s1 <= 0 && s2 <= 0 && s3 <= 0);
        }
    }
}
