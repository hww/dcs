using System;
using System.Collections.Generic;

namespace DCS.Spatial
{
    public sealed class RuntimeGeometryStorage
    {
        private SphereGeometry[] _spheres = Array.Empty<SphereGeometry>();
        private BoxGeometry[] _boxes = Array.Empty<BoxGeometry>();
        private TriangleGeometry[] _triangles = Array.Empty<TriangleGeometry>();

        // Загружаем чистые данные геометрии, без привязки к ScriptableObject карты
        public void Load(List<SphereGeometry> spheres, List<BoxGeometry> boxes, List<TriangleGeometry> triangles)
        {
            _spheres = Copy(spheres);
            _boxes = Copy(boxes);
            _triangles = Copy(triangles);
        }

        public void Clear()
        {
            _spheres = Array.Empty<SphereGeometry>();
            _boxes = Array.Empty<BoxGeometry>();
            _triangles = Array.Empty<TriangleGeometry>();
        }

        public ref readonly SphereGeometry GetSphere(int index) => ref _spheres[index];
        public ref readonly BoxGeometry GetBox(int index) => ref _boxes[index];
        public ref readonly TriangleGeometry GetTriangle(int index) => ref _triangles[index];

        private static T[] Copy<T>(List<T> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<T>();
            T[] result = new T[source.Count];
            for (int i = 0; i < source.Count; i++) result[i] = source[i];
            return result;
        }
    }
}
