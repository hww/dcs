using UnityEngine;

namespace DCS.Spatial
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
                    return IsPointInside(point, geometry.GetSphere(handle.Index));
                case EGeometryType.Box:
                    return IsPointInside(point, geometry.GetBox(handle.Index));
                case EGeometryType.Cylinder:
                    return IsPointInside(point, geometry.GetCylinder(handle.Index));
                case EGeometryType.Polygon:
                    return IsPointInside(point, handle.Index, geometry);
                default:
                    return false;
            }
        }

        public static bool IsPointInside(Vector3 point, in SphereGeometry sphere)
        {
            if (sphere.Radius < 0f)
                return false;

            Vector3 delta = point - sphere.Center;
            return delta.sqrMagnitude <= sphere.Radius * sphere.Radius;
        }

        public static bool IsPointInside(Vector3 point, in BoxGeometry box)
        {
            Quaternion inverse = Quaternion.Inverse(box.Rotation);
            Vector3 local = inverse * (point - box.Center);

            return Mathf.Abs(local.x) <= box.Extents.x &&
                   Mathf.Abs(local.y) <= box.Extents.y &&
                   Mathf.Abs(local.z) <= box.Extents.z;
        }

        public static bool IsPointInside(Vector3 point, in CylinderGeometry cylinder)
        {
            Quaternion inverse = Quaternion.Inverse(cylinder.Rotation);
            Vector3 local = inverse * (point - cylinder.Center);

            if (Mathf.Abs(local.y) > cylinder.HalfHeight)
                return false;

            return local.x * local.x + local.z * local.z <=
                   cylinder.Radius * cylinder.Radius;
        }

        private static bool IsPointInside(
            Vector3 point,
            int polygonIndex,
            RuntimeGeometryStorage geometry)
        {
            PolygonGeometry polygon = geometry.GetPolygon(polygonIndex);

            if (!polygon.Closed || polygon.PointCount < 3)
                return false;

            if (point.y < polygon.MinY || point.y > polygon.MaxY)
                return false;

            Vector2 p = new Vector2(point.x, point.z);
            bool inside = false;

            int end = polygon.StartIndex + polygon.PointCount;
            int j = end - 1;

            for (int i = polygon.StartIndex; i < end; i++)
            {
                Vector3 vi = geometry.GetPolygonPoint(i);
                Vector3 vj = geometry.GetPolygonPoint(j);

                bool intersects =
                    ((vi.z > point.z) != (vj.z > point.z)) &&
                    (point.x <
                     (vj.x - vi.x) * (point.z - vi.z) /
                     (vj.z - vi.z) + vi.x);

                if (intersects)
                    inside = !inside;

                j = i;
            }

            return inside;
        }
    }
}
