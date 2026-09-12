using UnityEngine;
namespace DynamicComponent
{
    public static class GeometryIntersection
    {
        public static bool IsPointInside(
            Vector3 point,
            GeometryHandle handle,
            RuntimeGeometryStorage geometry)
        {
            if (!handle.IsValid || geometry == null)
                return false;

            switch (handle.Type)
            {
                case EGeometryType.Sphere:
                    {
                        ref readonly SphereGeometry sphere =
                            ref geometry.GetSphere(handle.Index);

                        return IsPointInside(point, sphere);
                    }

                case EGeometryType.Box:
                    {
                        ref readonly BoxGeometry box =
                            ref geometry.GetBox(handle.Index);

                        return IsPointInside(point, box);
                    }

                case EGeometryType.Triangle:
                    {
                        ref readonly TriangleGeometry triangle =
                            ref geometry.GetTriangle(handle.Index);

                        return IsPointInside(point, triangle);
                    }

                default:
                    return false;
            }
        }

        public static bool IsPointInside(
            Vector3 point,
            in SphereGeometry sphere)
        {
            Vector3 delta = point - sphere.Center;

            return delta.sqrMagnitude <=
                   sphere.Radius * sphere.Radius;
        }

        public static bool IsPointInside(
            Vector3 point,
            in BoxGeometry box)
        {
            Quaternion inverseRotation =
                Quaternion.Inverse(box.Rotation);

            Vector3 localPoint =
                inverseRotation * (point - box.Center);

            return Mathf.Abs(localPoint.x) <= box.Extents.x &&
                   Mathf.Abs(localPoint.y) <= box.Extents.y &&
                   Mathf.Abs(localPoint.z) <= box.Extents.z;
        }

        public static bool IsPointInside(
            Vector3 point,
            in TriangleGeometry triangle)
        {
            // Polygon zones are currently evaluated in XZ.
            Vector2 p = new Vector2(point.x, point.z);

            Vector2 a = new Vector2(triangle.A.x, triangle.A.z);
            Vector2 b = new Vector2(triangle.B.x, triangle.B.z);
            Vector2 c = new Vector2(triangle.C.x, triangle.C.z);

            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);

            bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;

            return !(hasNegative && hasPositive);
        }

        private static float Sign(
            Vector2 p1,
            Vector2 p2,
            Vector2 p3)
        {
            return
                (p1.x - p3.x) * (p2.y - p3.y) -
                (p2.x - p3.x) * (p1.y - p3.y);
        }
    }
}