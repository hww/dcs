using System;
using UnityEngine;

namespace DCS.Spatial
{
    public sealed class RuntimeGeometryStorage
    {
        private SphereGeometry[] _spheres = Array.Empty<SphereGeometry>();
        private BoxGeometry[] _boxes = Array.Empty<BoxGeometry>();
        private CylinderGeometry[] _cylinders = Array.Empty<CylinderGeometry>();
        private PolygonGeometry[] _polygons = Array.Empty<PolygonGeometry>();
        private Vector3[] _polygonPoints = Array.Empty<Vector3>();

        public int SphereCount => _spheres.Length;
        public int BoxCount => _boxes.Length;
        public int CylinderCount => _cylinders.Length;
        public int PolygonCount => _polygons.Length;

        public void Load(
            SphereGeometry[] spheres,
            BoxGeometry[] boxes,
            CylinderGeometry[] cylinders,
            PolygonGeometry[] polygons,
            Vector3[] polygonPoints)
        {
            _spheres = spheres ?? Array.Empty<SphereGeometry>();
            _boxes = boxes ?? Array.Empty<BoxGeometry>();
            _cylinders = cylinders ?? Array.Empty<CylinderGeometry>();
            _polygons = polygons ?? Array.Empty<PolygonGeometry>();
            _polygonPoints = polygonPoints ?? Array.Empty<Vector3>();
        }

        public void Clear()
        {
            _spheres = Array.Empty<SphereGeometry>();
            _boxes = Array.Empty<BoxGeometry>();
            _cylinders = Array.Empty<CylinderGeometry>();
            _polygons = Array.Empty<PolygonGeometry>();
            _polygonPoints = Array.Empty<Vector3>();
        }

        public ref readonly SphereGeometry GetSphere(int index) => ref _spheres[index];
        public ref readonly BoxGeometry GetBox(int index) => ref _boxes[index];
        public ref readonly CylinderGeometry GetCylinder(int index) => ref _cylinders[index];
        public PolygonGeometry GetPolygon(int index) => _polygons[index];
        public Vector3 GetPolygonPoint(int index) => _polygonPoints[index];
    }
}
