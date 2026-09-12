using System;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicComponent
{

    public sealed class RuntimeGeometryStorage
    {
        private SphereGeometry[] _spheres = Array.Empty<SphereGeometry>();
        private BoxGeometry[] _boxes = Array.Empty<BoxGeometry>();
        private TriangleGeometry[] _triangles = Array.Empty<TriangleGeometry>();

        public void Load(MapDataset dataset)
        {
            if (dataset == null)
                throw new ArgumentNullException(nameof(dataset));

            _spheres = Copy(dataset.Spheres);
            _boxes = Copy(dataset.Boxes);
            _triangles = Copy(dataset.Triangles);
        }

        public void Clear()
        {
            _spheres = Array.Empty<SphereGeometry>();
            _boxes = Array.Empty<BoxGeometry>();
            _triangles = Array.Empty<TriangleGeometry>();
        }

        public ref readonly SphereGeometry GetSphere(int index)
        {
            return ref _spheres[index];
        }

        public ref readonly BoxGeometry GetBox(int index)
        {
            return ref _boxes[index];
        }

        public ref readonly TriangleGeometry GetTriangle(int index)
        {
            return ref _triangles[index];
        }

        private static T[] Copy<T>(List<T> source)
        {
            if (source == null || source.Count == 0)
                return Array.Empty<T>();

            T[] result = new T[source.Count];

            for (int i = 0; i < source.Count; i++)
                result[i] = source[i];

            return result;
        }
    }
}