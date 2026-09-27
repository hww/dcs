using UnityEngine;

namespace DCS.Spatial
{
    public sealed class RuntimeGeometryStorage
    {
        private SphereGeometry[] _spheres = System.Array.Empty<SphereGeometry>();
        private BoxGeometry[] _boxes = System.Array.Empty<BoxGeometry>();
        private CylinderGeometry[] _cylinders = System.Array.Empty<CylinderGeometry>();
        private PolygonGeometry[] _polygons = System.Array.Empty<PolygonGeometry>();
        private Vector3[] _polygonPoints = System.Array.Empty<Vector3>();

        public void Load(SphereGeometry[] spheres, BoxGeometry[] boxes, CylinderGeometry[] cylinders, PolygonGeometry[] polygons, Vector3[] polygonPoints)
        { _spheres = spheres ?? System.Array.Empty<SphereGeometry>(); _boxes = boxes ?? System.Array.Empty<BoxGeometry>(); _cylinders = cylinders ?? System.Array.Empty<CylinderGeometry>(); _polygons = polygons ?? System.Array.Empty<PolygonGeometry>(); _polygonPoints = polygonPoints ?? System.Array.Empty<Vector3>(); }
        public void Clear() { Load(null, null, null, null, null); }
        public ref readonly SphereGeometry GetSphere(int i) => ref _spheres[i];
        public ref readonly BoxGeometry GetBox(int i) => ref _boxes[i];
        public ref readonly CylinderGeometry GetCylinder(int i) => ref _cylinders[i];
        public ref readonly PolygonGeometry GetPolygon(int i) => ref _polygons[i];
        public Vector3 GetPolygonPoint(int i) => _polygonPoints[i];
    }
}
