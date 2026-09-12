#if UNITY_EDITOR
using UnityEngine;

namespace DynamicComponent.Packing
{
    public static class GeometryCompiler
    {
        /// <summary>
        /// Компиляция физического коллайдера в плоскую запись runtime-геометрии
        /// </summary>
        public static SpatialShapeRecord CompileCollider(ushort shapeId, ushort ownerId, EObjectType objType, Collider collider)
        {
            SpatialShapeRecord record = new SpatialShapeRecord
            {
                ShapeId = shapeId,
                OwnerId = ownerId,
                ObjectType = objType,
                Rotation = collider.transform.rotation,
                RawData0 = collider.transform.position
            };

            // Рассчитываем точный мировой AABB для Broadphase
            record.AABBMin = collider.bounds.min;
            record.AABBMax = collider.bounds.max;

            if (collider is BoxCollider box)
            {
                record.GeomType = EGeometryType.Box;
                record.RawData1 = Vector3.Scale(box.size * 0.5f, collider.transform.lossyScale); // Extents
                if (box.center != Vector3.zero)
                {
                    record.RawData0 += collider.transform.TransformDirection(box.center); // Смещение центра
                }
            }
            else if (collider is SphereCollider sphere)
            {
                record.GeomType = EGeometryType.Sphere;
                float maxScale = Mathf.Max(collider.transform.lossyScale.x, Mathf.Max(collider.transform.lossyScale.y, collider.transform.lossyScale.z));
                record.RawData1 = new Vector3(sphere.radius * maxScale, 0, 0); // Радиус пишем в X
                if (sphere.center != Vector3.zero)
                {
                    record.RawData0 += collider.transform.TransformDirection(sphere.center);
                }
            }
            else
            {
                // Если меш или капсула — для Narrowphase временно откатываемся к Box, но с ЧЕСТНЫМ мировым AABB
                record.GeomType = EGeometryType.Box;
                record.RawData1 = collider.bounds.extents;
                record.RawData0 = collider.bounds.center;
                record.Rotation = Quaternion.identity;
                Debug.LogWarning($"[GeometryCompiler] Коллайдер {collider.name} имеет неподдерживаемый тип. Запечен как точный AABB-Box.");
            }

            return record;
        }

        /// <summary>
        /// Компиляция одного треугольника полигональной зоны
        /// </summary>
        public static SpatialShapeRecord CompileTriangle(ushort shapeId, ushort ownerId, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            SpatialShapeRecord record = new SpatialShapeRecord
            {
                ShapeId = shapeId,
                OwnerId = ownerId,
                ObjectType = EObjectType.ZoneTrigger,
                GeomType = EGeometryType.Triangle,
                RawData0 = v0,
                RawData1 = v1,
                RawData2 = v2,
                Rotation = Quaternion.identity
            };

            // Находим точные границы треугольника для сетки
            record.AABBMin = Vector3.Min(v0, Vector3.Min(v1, v2));
            record.AABBMax = Vector3.Max(v0, Vector3.Max(v1, v2));

            return record;
        }
    }
}
#endif
