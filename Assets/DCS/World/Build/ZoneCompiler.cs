#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace DynamicComponent.Packing
{
    public static class ZoneCompiler
    {
        public static ZoneRecord CompileMetadata(Zone zone)
        {
            if (zone == null)
                return default;

            return new ZoneRecord
            {
                // Предполагаем, что у валидного Host ID всегда больше 0
                HostId = zone.Host.Id,
                Generation = zone.Host.Generation,
                Position = zone.transform.position,
                ActivationRadius = zone.ActivationRadius,
                FactsJson = zone.Facts != null ? zone.Facts.ToJson() : "{}"
            };
        }

        public static void CompileGeometry(Zone zone, MapDataset dataset)
        {
            if (zone == null || dataset == null)
                return;

            ushort ownerId = zone.Host.Id;
            bool hasGeometry = false;

            ZoneVolume[] volumes = zone.GetComponentsInChildren<ZoneVolume>(true);

            for (int i = 0; i < volumes.Length; i++)
            {
                ZoneVolume volume = volumes[i];
                Collider collider = volume.GetComponent<Collider>();

                if (collider == null)
                    continue;

                SpatialProxy proxy = GeometryCompiler.CompileCollider(
                    (ushort)dataset.SpatialProxies.Count,
                    ownerId,
                    EObjectType.ZoneTrigger,
                    collider,
                    dataset);

                dataset.SpatialProxies.Add(proxy);
                hasGeometry = true;
            }

            ZoneBorder border = zone.GetComponentInChildren<ZoneBorder>(true);

            if (border != null && border.Points != null && border.Points.Length >= 3)
            {
                CompileBorder(border, ownerId, dataset);
                hasGeometry = true;
            }

            if (!hasGeometry)
            {
                CompileFallbackSphere(zone, ownerId, dataset);
            }
        }

        private static void CompileBorder(ZoneBorder border, ushort ownerId, MapDataset dataset)
        {
            Vector3[] points = border.Points;
            if (points == null || points.Length < 3)
                return;

            Transform transform = border.transform;
            Vector3 first = transform.TransformPoint(points[0]);

            for (int i = 1; i < points.Length - 1; i++)
            {
                Vector3 b = transform.TransformPoint(points[i]);
                Vector3 c = transform.TransformPoint(points[i + 1]);

                SpatialProxy proxy = GeometryCompiler.CompileTriangle(
                    (ushort)dataset.SpatialProxies.Count,
                    ownerId,
                    EObjectType.ZoneTrigger,
                    first,
                    b,
                    c,
                    dataset);

                dataset.SpatialProxies.Add(proxy);
            }
        }

        private static void CompileFallbackSphere(Zone zone, ushort ownerId, MapDataset dataset)
        {
            Vector3 center = zone.transform.position;
            float radius = Mathf.Max(0.1f, zone.ZoneRadius);
            int geometryIndex = dataset.Spheres.Count;

            dataset.Spheres.Add(new SphereGeometry
            {
                Center = center,
                Radius = radius
            });

            SpatialProxy proxy = new SpatialProxy
            {
                ShapeId = (ushort)dataset.SpatialProxies.Count,
                OwnerId = ownerId,
                ObjectType = EObjectType.ZoneTrigger,
                Geometry = new GeometryHandle
                {
                    Type = EGeometryType.Sphere,
                    Index = geometryIndex
                },
                AABBMin = center - Vector3.one * radius,
                AABBMax = center + Vector3.one * radius
            };

            dataset.SpatialProxies.Add(proxy);
        }
    }
}
#endif
