#if UNITY_EDITOR

using UnityEngine;

namespace DynamicComponent.Packing
{
    public static class GeometryCompiler
    {
        /// <summary>
        /// Компилирует Unity Collider в runtime geometry
        /// и создаёт соответствующий SpatialProxy.
        /// </summary>
        public static SpatialProxy CompileCollider(
            ushort shapeId,
            ushort ownerId,
            EObjectType objectType,
            Collider collider,
            MapDataset dataset)
        {
            if (collider is BoxCollider box)
            {
                return CompileBox(
                    shapeId,
                    ownerId,
                    objectType,
                    box,
                    dataset);
            }

            if (collider is SphereCollider sphere)
            {
                return CompileSphere(
                    shapeId,
                    ownerId,
                    objectType,
                    sphere,
                    dataset);
            }

            Debug.LogWarning(
                $"[GeometryCompiler] " +
                $"Коллайдер '{collider.name}' " +
                $"имеет неподдерживаемый тип. " +
                $"Будет использован AABB.");

            return CompileFallbackBox(
                shapeId,
                ownerId,
                objectType,
                collider,
                dataset);
        }


        private static SpatialProxy CompileSphere(
            ushort shapeId,
            ushort ownerId,
            EObjectType objectType,
            SphereCollider collider,
            MapDataset dataset)
        {
            Transform transform = collider.transform;

            Vector3 center =
                transform.TransformPoint(collider.center);

            Vector3 scale = transform.lossyScale;

            float maxScale = Mathf.Max(
                Mathf.Abs(scale.x),
                Mathf.Max(
                    Mathf.Abs(scale.y),
                    Mathf.Abs(scale.z)));

            float radius = collider.radius * maxScale;

            int geometryIndex = dataset.Spheres.Count;

            dataset.Spheres.Add(new SphereGeometry
            {
                Center = center,
                Radius = radius
            });

            GeometryHandle handle =
                new GeometryHandle(
                    EGeometryType.Sphere,
                    geometryIndex);

            return CreateProxy(
                shapeId,
                ownerId,
                objectType,
                handle,
                collider.bounds);
        }


        private static SpatialProxy CompileBox(
            ushort shapeId,
            ushort ownerId,
            EObjectType objectType,
            BoxCollider collider,
            MapDataset dataset)
        {
            Transform transform = collider.transform;

            Vector3 center =
                transform.TransformPoint(collider.center);

            Vector3 scale = transform.lossyScale;

            Vector3 extents = new Vector3(
                Mathf.Abs(collider.size.x * scale.x) * 0.5f,
                Mathf.Abs(collider.size.y * scale.y) * 0.5f,
                Mathf.Abs(collider.size.z * scale.z) * 0.5f);

            int geometryIndex = dataset.Boxes.Count;

            dataset.Boxes.Add(new BoxGeometry
            {
                Center = center,
                Extents = extents,
                Rotation = transform.rotation
            });

            GeometryHandle handle =
                new GeometryHandle(
                    EGeometryType.Box,
                    geometryIndex);

            return CreateProxy(
                shapeId,
                ownerId,
                objectType,
                handle,
                collider.bounds);
        }


        /// <summary>
        /// Временный fallback.
        ///
        /// Это НЕ точная геометрия исходного collider.
        /// Мы явно превращаем его в AABB Box.
        /// </summary>
        private static SpatialProxy CompileFallbackBox(
            ushort shapeId,
            ushort ownerId,
            EObjectType objectType,
            Collider collider,
            MapDataset dataset)
        {
            Bounds bounds = collider.bounds;

            int geometryIndex = dataset.Boxes.Count;

            dataset.Boxes.Add(new BoxGeometry
            {
                Center = bounds.center,
                Extents = bounds.extents,
                Rotation = Quaternion.identity
            });

            GeometryHandle handle =
                new GeometryHandle(
                    EGeometryType.Box,
                    geometryIndex);

            return CreateProxy(
                shapeId,
                ownerId,
                objectType,
                handle,
                bounds);
        }


        public static SpatialProxy CompileTriangle(
            ushort shapeId,
            ushort ownerId,
            EObjectType objectType,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            MapDataset dataset)
        {
            int geometryIndex = dataset.Triangles.Count;

            dataset.Triangles.Add(new TriangleGeometry
            {
                A = a,
                B = b,
                C = c
            });

            Vector3 min =
                Vector3.Min(
                    a,
                    Vector3.Min(b, c));

            Vector3 max =
                Vector3.Max(
                    a,
                    Vector3.Max(b, c));

            GeometryHandle handle =
                new GeometryHandle(
                    EGeometryType.Triangle,
                    geometryIndex);

            return new SpatialProxy(
                shapeId,
                ownerId,
                objectType,
                handle,
                min,
                max);
        }


        private static SpatialProxy CreateProxy(
            ushort shapeId,
            ushort ownerId,
            EObjectType objectType,
            GeometryHandle handle,
            Bounds bounds)
        {
            return new SpatialProxy(
                shapeId,
                ownerId,
                objectType,
                handle,
                bounds.min,
                bounds.max);
        }
    }
}

#endif