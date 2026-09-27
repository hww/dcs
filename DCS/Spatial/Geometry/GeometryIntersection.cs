using UnityEngine;

namespace DCS.Spatial
{
    public static class GeometryIntersection
    {
        public static bool IsPointInside(Vector3 point, GeometryHandle handle, RuntimeGeometryStorage geometry)
        {
            if (!handle.IsValid || geometry == null) return false;
            switch (handle.Type)
            {
                case EGeometryType.Sphere: return IsPointInside(point, geometry.GetSphere(handle.Index));
                case EGeometryType.Box: return IsPointInside(point, geometry.GetBox(handle.Index));
                case EGeometryType.Cylinder: return IsPointInside(point, geometry.GetCylinder(handle.Index));
                case EGeometryType.Polygon: return IsPointInsidePolygon(point, handle.Index, geometry);
                default: return false;
            }
        }

        public static bool IsPointInside(Vector3 p, in SphereGeometry s)
        {
            if (s.Radius < 0f) return false;
            Vector3 d = p - s.Center;
            return d.sqrMagnitude <= s.Radius * s.Radius;
        }

        public static bool IsPointInside(Vector3 p, in BoxGeometry b)
        {
            Vector3 local = Quaternion.Inverse(b.Rotation) * (p - b.Center);
            return Mathf.Abs(local.x) <= b.Extents.x && Mathf.Abs(local.y) <= b.Extents.y && Mathf.Abs(local.z) <= b.Extents.z;
        }

        public static bool IsPointInside(Vector3 p, in CylinderGeometry c)
        {
            Vector3 local = Quaternion.Inverse(c.Rotation) * (p - c.Center);
            return Mathf.Abs(local.y) <= c.HalfHeight && local.x * local.x + local.z * local.z <= c.Radius * c.Radius;
        }

        private static bool IsPointInsidePolygon(Vector3 p, int index, RuntimeGeometryStorage g)
        {
            PolygonGeometry poly = g.GetPolygon(index);
            if (!poly.Closed || poly.PointCount < 3 || p.y < poly.MinY || p.y > poly.MaxY) return false;
            bool inside = false;
            int start = poly.StartIndex;
            int end = start + poly.PointCount;
            int j = end - 1;
            for (int i = start; i < end; i++)
            {
                Vector3 a = g.GetPolygonPoint(i);
                Vector3 b = g.GetPolygonPoint(j);
                bool crosses = ((a.z > p.z) != (b.z > p.z)) &&
                               (p.x < (b.x - a.x) * (p.z - a.z) / (b.z - a.z) + a.x);
                if (crosses) inside = !inside;
                j = i;
            }
            return inside;
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, GeometryHandle handle, RuntimeGeometryStorage g, out Vector3 position, out Vector3 normal, out float distance)
        {
            position = default; normal = default; distance = 0f;
            if (!handle.IsValid || g == null || maxDistance < 0f) return false;
            switch (handle.Type)
            {
                case EGeometryType.Sphere: return RaySphere(origin, direction, maxDistance, g.GetSphere(handle.Index), out position, out normal, out distance);
                case EGeometryType.Box: return RayBox(origin, direction, maxDistance, g.GetBox(handle.Index), out position, out normal, out distance);
                case EGeometryType.Cylinder: return RayCylinder(origin, direction, maxDistance, g.GetCylinder(handle.Index), out position, out normal, out distance);
                case EGeometryType.Polygon: return RayPolygonPrism(origin, direction, maxDistance, handle.Index, g, out position, out normal, out distance);
                default: return false;
            }
        }

        private static bool RaySphere(Vector3 o, Vector3 d, float max, in SphereGeometry s, out Vector3 p, out Vector3 n, out float t)
        {
            p = default; n = default; t = 0f;
            float a = Vector3.Dot(d, d);
            if (a <= 1e-8f) return false;
            Vector3 m = o - s.Center;
            float b = Vector3.Dot(m, d);
            float c = Vector3.Dot(m, m) - s.Radius * s.Radius;
            float disc = b * b - a * c;
            if (disc < 0f) return false;
            float root = Mathf.Sqrt(disc);
            float t0 = (-b - root) / a;
            float t1 = (-b + root) / a;
            float hit = t0 >= 0f ? t0 : t1;
            if (hit < 0f || hit > max) return false;
            t = hit; p = o + d * t; n = (p - s.Center).normalized; return true;
        }

        private static bool RayBox(Vector3 o, Vector3 d, float max, in BoxGeometry b, out Vector3 p, out Vector3 n, out float t)
        {
            p = default; n = default; t = 0f;
            Quaternion inv = Quaternion.Inverse(b.Rotation);
            Vector3 lo = inv * (o - b.Center);
            Vector3 ld = inv * d;
            float tMin = 0f, tMax = max;
            int hitAxis = -1; float hitSign = 0f;
            if (!Slab(lo.x, ld.x, -b.Extents.x, b.Extents.x, ref tMin, ref tMax, ref hitAxis, ref hitSign, 0) ||
                !Slab(lo.y, ld.y, -b.Extents.y, b.Extents.y, ref tMin, ref tMax, ref hitAxis, ref hitSign, 1) ||
                !Slab(lo.z, ld.z, -b.Extents.z, b.Extents.z, ref tMin, ref tMax, ref hitAxis, ref hitSign, 2)) return false;
            t = tMin;
            p = o + d * t;
            Vector3 localNormal = hitAxis == 0 ? new Vector3(hitSign, 0f, 0f) : hitAxis == 1 ? new Vector3(0f, hitSign, 0f) : new Vector3(0f, 0f, hitSign);
            n = b.Rotation * localNormal;
            return true;
        }

        private static bool Slab(float o, float d, float min, float max, ref float tMin, ref float tMax, ref int axis, ref float sign, int currentAxis)
        {
            if (Mathf.Abs(d) < 1e-7f) return o >= min && o <= max;
            float a = (min - o) / d;
            float b = (max - o) / d;
            float enteringSign = -1f;
            if (a > b) { float tmp = a; a = b; b = tmp; enteringSign = 1f; }
            if (a > tMin) { tMin = a; axis = currentAxis; sign = enteringSign; }
            tMax = Mathf.Min(tMax, b);
            return tMin <= tMax && tMax >= 0f;
        }

        private static bool RayCylinder(Vector3 o, Vector3 d, float max, in CylinderGeometry c, out Vector3 p, out Vector3 n, out float t)
        {
            p = default; n = default; t = 0f;
            Quaternion inv = Quaternion.Inverse(c.Rotation);
            Vector3 lo = inv * (o - c.Center);
            Vector3 ld = inv * d;
            float best = float.PositiveInfinity; Vector3 bestNormal = default;

            float a = ld.x * ld.x + ld.z * ld.z;
            float b = 2f * (lo.x * ld.x + lo.z * ld.z);
            float cc = lo.x * lo.x + lo.z * lo.z - c.Radius * c.Radius;
            float disc = b * b - 4f * a * cc;
            if (a > 1e-8f && disc >= 0f)
            {
                float root = Mathf.Sqrt(disc);
                float r0 = (-b - root) / (2f * a);
                float r1 = (-b + root) / (2f * a);
                if (r0 > r1) { float q = r0; r0 = r1; r1 = q; }
                if (r0 >= 0f && r0 <= max)
                {
                    float y = lo.y + ld.y * r0;
                    if (Mathf.Abs(y) <= c.HalfHeight) { best = r0; bestNormal = new Vector3(lo.x + ld.x * r0, 0f, lo.z + ld.z * r0).normalized; }
                }
                if (float.IsPositiveInfinity(best) && r1 >= 0f && r1 <= max)
                {
                    float y = lo.y + ld.y * r1;
                    if (Mathf.Abs(y) <= c.HalfHeight) { best = r1; bestNormal = new Vector3(lo.x + ld.x * r1, 0f, lo.z + ld.z * r1).normalized; }
                }
            }

            if (Mathf.Abs(ld.y) > 1e-8f)
            {
                float bottom = (-c.HalfHeight - lo.y) / ld.y;
                if (bottom >= 0f && bottom <= max)
                {
                    float x = lo.x + ld.x * bottom, z = lo.z + ld.z * bottom;
                    if (x * x + z * z <= c.Radius * c.Radius && bottom < best) { best = bottom; bestNormal = Vector3.down; }
                }
                float top = (c.HalfHeight - lo.y) / ld.y;
                if (top >= 0f && top <= max)
                {
                    float x = lo.x + ld.x * top, z = lo.z + ld.z * top;
                    if (x * x + z * z <= c.Radius * c.Radius && top < best) { best = top; bestNormal = Vector3.up; }
                }
            }
            if (float.IsPositiveInfinity(best)) return false;
            t = best; p = o + d * t; n = c.Rotation * bestNormal; return true;
        }

        private static bool RayPolygonPrism(Vector3 o, Vector3 d, float max, int index, RuntimeGeometryStorage g, out Vector3 p, out Vector3 n, out float t)
        {
            p = default; n = default; t = 0f;
            PolygonGeometry poly = g.GetPolygon(index);
            if (!poly.Closed || poly.PointCount < 3) return false;
            float best = float.PositiveInfinity; Vector3 bestNormal = default;

            if (Mathf.Abs(d.y) > 1e-8f)
            {
                float q = (poly.MinY - o.y) / d.y;
                if (q >= 0f && q <= max) { Vector3 x = o + d * q; if (IsPointInsidePolygon(new Vector3(x.x, (poly.MinY + poly.MaxY) * .5f, x.z), index, g) && q < best) { best = q; bestNormal = Vector3.down; } }
                q = (poly.MaxY - o.y) / d.y;
                if (q >= 0f && q <= max) { Vector3 x = o + d * q; if (IsPointInsidePolygon(new Vector3(x.x, (poly.MinY + poly.MaxY) * .5f, x.z), index, g) && q < best) { best = q; bestNormal = Vector3.up; } }
            }

            for (int i = 0; i < poly.PointCount; i++)
            {
                Vector3 a = g.GetPolygonPoint(poly.StartIndex + i);
                Vector3 b = g.GetPolygonPoint(poly.StartIndex + ((i + 1) % poly.PointCount));
                Vector2 A = new Vector2(a.x, a.z), B = new Vector2(b.x, b.z), O = new Vector2(o.x, o.z), D = new Vector2(d.x, d.z);
                float den = D.x * (B.y - A.y) - D.y * (B.x - A.x);
                if (Mathf.Abs(den) < 1e-8f) continue;
                Vector2 AO = A - O;
                float qRay = (AO.x * (B.y - A.y) - AO.y * (B.x - A.x)) / den;
                float uSeg = (AO.x * D.y - AO.y * D.x) / den;
                if (qRay < 0f || qRay > max || uSeg < 0f || uSeg > 1f) continue;
                float y = o.y + d.y * qRay;
                if (y < poly.MinY || y > poly.MaxY || qRay >= best) continue;
                Vector3 edge = b - a;
                Vector3 side = Vector3.Cross(Vector3.up, edge).normalized;
                best = qRay; bestNormal = Vector3.Dot(side, d) > 0f ? -side : side;
            }
            if (float.IsPositiveInfinity(best)) return false;
            t = best; p = o + d * t; n = bestNormal; return true;
        }
    }
}
