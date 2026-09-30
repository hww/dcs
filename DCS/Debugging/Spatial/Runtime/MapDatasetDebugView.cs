using System.Collections.Generic;
using DCS.Data;
using UnityEngine;

namespace DCS.Debugging.Runtime
{
    /// <summary>
    /// Scene component that draws the contents of a MapDataset asset using
    /// Gizmos. Works in both Edit Mode and Play Mode thanks to
    /// [ExecuteAlways]. Contains no editor-only code; labels and the HUD are
    /// drawn by a separate editor-side overlay.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("DCS/Debug/Map Dataset Debug View")]
    public sealed class MapDatasetDebugView : MonoBehaviour
    {
        [SerializeField] private MapDataset _dataset;
        [SerializeField] private DebugDrawSettings _settings = new DebugDrawSettings();

        public MapDataset Dataset
        {
            get => _dataset;
            set => _dataset = value;
        }

        public DebugDrawSettings Settings => _settings;

        // Cached, rebuilt lazily. Avoids re-touching the asset every frame.
        private readonly List<Vector3> _scratchPoints = new List<Vector3>(256);

        // -----------------------------------------------------------------
        // Gizmo entry point
        // -----------------------------------------------------------------

        private void OnDrawGizmos()
        {
            if (_dataset == null)
                return;

            var s = _settings;

            // The dataset stores world-space positions, so we intentionally
            // reset the gizmo matrix to identity instead of using this
            // transform. Reposition the GameObject to visually offset data.
            var prevMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.identity;

            if (s.ShowSpawners) DrawSpawners(_dataset, s);
            if (s.ShowLocators) DrawLocators(_dataset, s);
            if (s.ShowPatrolPoints) DrawPatrolPoints(_dataset, s);
            if (s.ShowStrongPoints) DrawStrongPoints(_dataset, s);
            if (s.ShowPosts) DrawPosts(_dataset, s);
            if (s.ShowTraversalLinks) DrawTraversalLinks(_dataset, s);
            if (s.ShowNavigationSurfaces) DrawNavigationSurfaces(_dataset, s);
            if (s.ShowPolygons) DrawPolygons(_dataset, s);
            if (s.ShowSpheres) DrawSpheres(_dataset, s);
            if (s.ShowBoxes) DrawBoxes(_dataset, s);
            if (s.ShowCylinders) DrawCylinders(_dataset, s);

            Gizmos.matrix = prevMatrix;
        }

        // -----------------------------------------------------------------
        // Individual layers
        // -----------------------------------------------------------------

        private void DrawSpawners(MapDataset d, DebugDrawSettings s)
        {
            for (int i = 0; i < d.Spawners.Count; i++)
            {
                var sp = d.Spawners[i];
                Gizmos.color = s.SpawnerColor;
                float size = s.MarkerSize;

                // A cube with a vertical "tail" so it reads as a spawn anchor.
                Gizmos.DrawWireCube(sp.Position, Vector3.one * size);
                Gizmos.DrawLine(sp.Position, sp.Position + Vector3.up * size * 2f);
                Gizmos.DrawWireSphere(sp.Position + Vector3.up * size * 2f, size * 0.15f);
            }
        }

        private void DrawLocators(MapDataset d, DebugDrawSettings s)
        {
            Gizmos.color = s.LocatorColor;
            for (int i = 0; i < d.Locators.Count; i++)
            {
                var l = d.Locators[i];
                Gizmos.DrawWireSphere(l.Position, s.MarkerSize * 0.5f);

                // Two crossed lines to make the locator stand out from spheres.
                float r = s.MarkerSize * 0.5f;
                Gizmos.DrawLine(l.Position - Vector3.right * r, l.Position + Vector3.right * r);
                Gizmos.DrawLine(l.Position - Vector3.up * r, l.Position + Vector3.up * r);
            }
        }

        private void DrawPatrolPoints(MapDataset d, DebugDrawSettings s)
        {
            var pts = d.PatrolPoints;
            if (pts == null || pts.Count == 0)
                return;

            Gizmos.color = s.PatrolColor;

            // Polyline through all points.
            for (int i = 1; i < pts.Count; i++)
                Gizmos.DrawLine(pts[i - 1], pts[i]);

            // Markers.
            for (int i = 0; i < pts.Count; i++)
                Gizmos.DrawSphere(pts[i], s.MarkerSize * 0.15f);

            // PatrolPathRecord defines ranges into PatrolPoints. Draw each
            // path's polyline in a brighter shade so ranges are visible.
            var bright = s.PatrolColor;
            bright.a = 1f;
            Gizmos.color = bright;

            for (int i = 0; i < d.PatrolPaths.Count; i++)
            {
                var path = d.PatrolPaths[i];
                int start = path.StartPoint;
                int count = path.PointCount;
                if (count < 2) continue;
                if (start < 0 || start + count > pts.Count) continue;

                int end = start + count - 1;
                for (int j = start; j < end; j++)
                    Gizmos.DrawLine(pts[j], pts[j + 1]);

                if (path.Closed)
                    Gizmos.DrawLine(pts[end], pts[start]);
            }
        }

        private void DrawStrongPoints(MapDataset d, DebugDrawSettings s)
        {
            for (int i = 0; i < d.StrongPoints.Count; i++)
            {
                var sp = d.StrongPoints[i];
                Gizmos.color = s.Resolve(s.StrongPointColor, sp.Enabled);

                float size = s.MarkerSize;
                Gizmos.DrawWireCube(sp.Position, Vector3.one * size);

                // Orientation indicator.
                Vector3 fwd = sp.Rotation * Vector3.forward * size;
                Gizmos.DrawLine(sp.Position, sp.Position + fwd);
            }
        }

        private void DrawPosts(MapDataset d, DebugDrawSettings s)
        {
            for (int i = 0; i < d.Posts.Count; i++)
            {
                var p = d.Posts[i];
                Gizmos.color = s.Resolve(s.PostColor, p.Enabled);

                Gizmos.DrawWireSphere(p.Position, s.MarkerSize * 0.4f);

                // Post type encoded as a tiny vertical tick count.
                int ticks = Mathf.Clamp(p.Type, 0, 8);
                for (int t = 0; t < ticks; t++)
                {
                    Vector3 a = p.Position + Vector3.up * (s.MarkerSize * (0.5f + t * 0.15f));
                    Vector3 b = a + Vector3.up * (s.MarkerSize * 0.1f);
                    Gizmos.DrawLine(a, b);
                }
            }
        }

        private void DrawTraversalLinks(MapDataset d, DebugDrawSettings s)
        {
            for (int i = 0; i < d.TraversalLinks.Count; i++)
            {
                var l = d.TraversalLinks[i];
                Gizmos.color = s.Resolve(s.TraversalColor, l.Enabled);

                Gizmos.DrawLine(l.Start, l.End);
                Gizmos.DrawWireSphere(l.Start, s.MarkerSize * 0.2f);
                Gizmos.DrawWireSphere(l.End, s.MarkerSize * 0.2f);

                // Small arrowhead at the end so direction is visible.
                Vector3 dir = (l.End - l.Start);
                float len = dir.magnitude;
                if (len > 0.001f)
                {
                    dir /= len;
                    Vector3 side = Vector3.Cross(dir, Vector3.up).normalized * s.MarkerSize * 0.2f;
                    Vector3 back = l.End - dir * s.MarkerSize * 0.4f;
                    Gizmos.DrawLine(l.End, back + side);
                    Gizmos.DrawLine(l.End, back - side);
                }
            }
        }

        private void DrawNavigationSurfaces(MapDataset d, DebugDrawSettings s)
        {
            // NavigationSurfaceRecord carries ShapeId which points into a
            // separate geometry registry that is not part of MapDataset.
            // Draw a marker at each surface's associated shape, if any.
            // If you have a ShapeId -> geometry lookup, plug it in here.
            Gizmos.color = s.NavigationColor;
            for (int i = 0; i < d.NavigationSurfaces.Count; i++)
            {
                var ns = d.NavigationSurfaces[i];
                // No world position on the record itself; nothing to draw.
                // The shape it references is visualized by the shape layer.
            }
        }

        private void DrawPolygons(MapDataset d, DebugDrawSettings s)
        {
            var pts = d.PolygonPoints;
            if (pts == null || pts.Count == 0)
                return;

            Gizmos.color = s.PolygonColor;

            for (int i = 0; i < d.Polygons.Count; i++)
            {
                var poly = d.Polygons[i];

                int start = poly.StartIndex;
                int count = poly.PointCount;

                if (count < 2) continue;
                if (start < 0 || start + count > pts.Count) continue;

                DrawPrism(pts, start, count, poly.MinY, poly.MaxY, poly.Closed);
            }

            var marker = s.PolygonColor;
            marker.a = 1f;
            Gizmos.color = marker;

            for (int i = 0; i < pts.Count; i++)
                Gizmos.DrawSphere(new Vector3(pts[i].x, d.Polygons[0].MinY, pts[i].z), s.MarkerSize * 0.08f);
        }

        private static void DrawPrism(
            List<Vector3> pts, int start, int count, float minY, float maxY, bool closed)
        {
            int last = start + count - 1;

            for (int j = start; j < last; j++)
            {
                Vector3 a = pts[j];
                Vector3 b = pts[j + 1];

                Gizmos.DrawLine(new Vector3(a.x, minY, a.z), new Vector3(b.x, minY, b.z));
                Gizmos.DrawLine(new Vector3(a.x, maxY, a.z), new Vector3(b.x, maxY, b.z));
            }

            if (closed && count > 2)
            {
                Vector3 a = pts[last];
                Vector3 b = pts[start];
                Gizmos.DrawLine(new Vector3(a.x, minY, a.z), new Vector3(b.x, minY, b.z));
                Gizmos.DrawLine(new Vector3(a.x, maxY, a.z), new Vector3(b.x, maxY, b.z));
            }

            for (int j = start; j <= last; j++)
            {
                Vector3 p = pts[j];
                Gizmos.DrawLine(
                    new Vector3(p.x, minY, p.z),
                    new Vector3(p.x, maxY, p.z));
            }
        }

        private void DrawSpheres(MapDataset d, DebugDrawSettings s)
        {
            Gizmos.color = s.SphereColor;
            for (int i = 0; i < d.Spheres.Count; i++)
            {
                var g = d.Spheres[i];
                Gizmos.DrawWireSphere(g.Center, g.Radius);
            }
        }

        private void DrawBoxes(MapDataset d, DebugDrawSettings s)
        {
            Gizmos.color = s.BoxColor;
            for (int i = 0; i < d.Boxes.Count; i++)
            {
                var g = d.Boxes[i];
                var prev = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(g.Center, g.Rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, g.Extents);
                Gizmos.matrix = prev;
            }
        }

        private void DrawCylinders(MapDataset d, DebugDrawSettings s)
        {
            Gizmos.color = s.CylinderColor;
            for (int i = 0; i < d.Cylinders.Count; i++)
            {
                var g = d.Cylinders[i];
                DrawWireCylinder(g.Center, g.Rotation, g.Radius, g.Height, s.CircleSegments);
            }
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Draws a wireframe cylinder using two circles and vertical struts.
        /// </summary>
        private static void DrawWireCylinder(
            Vector3 center, Quaternion rotation, float radius, float height, int segments)
        {
            segments = Mathf.Max(4, segments);
            float halfH = height * 0.5f;

            Vector3 up = rotation * Vector3.up;
            Vector3 right = rotation * Vector3.right;
            Vector3 fwd = rotation * Vector3.forward;

            Vector3 prevTop = Vector3.zero;
            Vector3 prevBot = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                float a = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 radial = right * (Mathf.Cos(a) * radius) + fwd * (Mathf.Sin(a) * radius);

                Vector3 top = center + up * halfH + radial;
                Vector3 bot = center + up * -halfH + radial;

                if (i > 0)
                {
                    Gizmos.DrawLine(prevTop, top);
                    Gizmos.DrawLine(prevBot, bot);
                }

                // Vertical struts every quarter turn keeps the shape readable.
                if (i % Mathf.Max(1, segments / 4) == 0)
                    Gizmos.DrawLine(bot, top);

                prevTop = top;
                prevBot = bot;
            }
        }

        // -----------------------------------------------------------------
        // Public helpers used by the editor overlay / window
        // -----------------------------------------------------------------

        /// <summary>
        /// Computes a world-space bounds enclosing every positional entry in
        /// the dataset. Used by "Frame All".
        /// </summary>
        /// <summary>
        /// Computes a world-space bounds enclosing every positional entry in
        /// the dataset. Used by "Frame All".
        /// </summary>
        public static bool TryGetBounds(MapDataset d, out Bounds bounds)
        {
            bounds = default;
            if (d == null) return false;

            // Local mutable state; the out parameter cannot be captured by a
            // local function (CS1628), so we accumulate here and copy at the end.
            var acc = new Bounds();
            bool has = false;

            void Encapsulate(Vector3 p)
            {
                if (!has)
                {
                    acc = new Bounds(p, Vector3.zero);
                    has = true;
                }
                else
                {
                    acc.Encapsulate(p);
                }
            }

            for (int i = 0; i < d.Spawners.Count; i++) Encapsulate(d.Spawners[i].Position);
            for (int i = 0; i < d.Locators.Count; i++) Encapsulate(d.Locators[i].Position);
            for (int i = 0; i < d.PatrolPoints.Count; i++) Encapsulate(d.PatrolPoints[i]);
            for (int i = 0; i < d.StrongPoints.Count; i++) Encapsulate(d.StrongPoints[i].Position);
            for (int i = 0; i < d.Posts.Count; i++) Encapsulate(d.Posts[i].Position);
            for (int i = 0; i < d.TraversalLinks.Count; i++)
            {
                Encapsulate(d.TraversalLinks[i].Start);
                Encapsulate(d.TraversalLinks[i].End);
            }
            for (int i = 0; i < d.PolygonPoints.Count; i++) Encapsulate(d.PolygonPoints[i]);
            for (int i = 0; i < d.Spheres.Count; i++) Encapsulate(d.Spheres[i].Center);
            for (int i = 0; i < d.Boxes.Count; i++) Encapsulate(d.Boxes[i].Center);
            for (int i = 0; i < d.Cylinders.Count; i++) Encapsulate(d.Cylinders[i].Center);

            if (has)
                bounds = acc;

            return has;
        }
    }
}