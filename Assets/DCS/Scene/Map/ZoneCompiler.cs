#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace DynamicComponent.Packing
{
    public static class ZoneCompiler
    {
        public static ZoneRecord CompileMetadata(Zone zone)
        {
            return new ZoneRecord
            {
                HostId = zone.Host.Id,
                Generation = zone.Host.Generation,
                Position = zone.transform.position,
                ActivationRadius = zone.ActivationRadius,
                FactsJson = zone.Facts != null ? zone.Facts.ToJson() : "{}"
            };
        }

        public static void CompileGeometry(Zone zone, ref ushort globalShapeIdCounter, List<SpatialShapeRecord> outShapes)
        {
            ushort ownerId = zone.Host.Id;

            // 1. Собираем геометрию из дочерних объемов (Box / Sphere)
            var volumes = zone.GetComponentsInChildren<ZoneVolume>(true);
            if (volumes != null && volumes.Length > 0)
            {
                foreach (var vol in volumes)
                {
                    var collider = vol.GetComponent<Collider>();
                    if (collider != null)
                    {
                        globalShapeIdCounter++;
                        var shape = GeometryCompiler.CompileCollider(globalShapeIdCounter, ownerId, EObjectType.ZoneTrigger, collider);
                        outShapes.Add(shape);
                    }
                }
            }

            // 2. Собираем геометрию из полигональных границ (ZoneBorder) -> превращаем в Треугольники
            var border = zone.GetComponentInChildren<ZoneBorder>(true);
            if (border != null && border.Points != null && border.Points.Length >= 3)
            {
                // Переводим локальные точки дизайнера в мировые координаты один раз при запекании
                Vector3[] worldPoints = new Vector3[border.Points.Length];
                for (int i = 0; i < border.Points.Length; i++)
                {
                    worldPoints[i] = border.transform.TransformPoint(border.Points[i]);
                }

                // Триангуляция веером (для MVP): берем первую точку и соединяем с последующими
                Vector3 rootVertex = worldPoints[0];
                for (int i = 1; i < worldPoints.Length - 1; i++)
                {
                    globalShapeIdCounter++;
                    var triangleShape = GeometryCompiler.CompileTriangle(
                        globalShapeIdCounter,
                        ownerId,
                        rootVertex,
                        worldPoints[i],
                        worldPoints[i + 1]
                    );
                    outShapes.Add(triangleShape);
                }
            }

            // 3. Fallback: Если геймдизайнер не создал геометрию, строим базовую сферу по ZoneRadius
            if ((volumes == null || volumes.Length == 0) && border == null)
            {
                globalShapeIdCounter++;
                SpatialShapeRecord baseSphere = new SpatialShapeRecord
                {
                    ShapeId = globalShapeIdCounter,
                    OwnerId = ownerId,
                    ObjectType = EObjectType.ZoneTrigger,
                    GeomType = EGeometryType.Sphere,
                    RawData0 = zone.transform.position,
                    RawData1 = new Vector3(zone.ZoneRadius, 0, 0),
                    Rotation = Quaternion.identity,
                    AABBMin = zone.transform.position - new Vector3(zone.ZoneRadius, zone.ZoneRadius, zone.ZoneRadius),
                    AABBMax = zone.transform.position + new Vector3(zone.ZoneRadius, zone.ZoneRadius, zone.ZoneRadius)
                };
                outShapes.Add(baseSphere);
            }
        }
    }
}
#endif
