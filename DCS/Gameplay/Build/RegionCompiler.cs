#if UNITY_EDITOR
using DCS.Core;
using DCS.Gameplay;
using DCS.Gameplay.Build;
using DCS.Interaction.Authoring;
using DCS.Spatial;
using UnityEngine;

namespace DCS.Core.Packing
{
    public static class RegionCompiler
    {
        public static bool TryCompile(
            Region region,
            MapDataset dataset,
            ushort regionId,
            ushort encounterId,
            ushort strongPointId)
        {
            if (region == null || dataset == null || !region.Enabled)
                return false;

            BaseShape[] shapes = GetDirectShapes(region);

            dataset.Regions.Add(new RegionRecord
            {
                Id = regionId,
                EncounterId = encounterId,
                StrongPointId = strongPointId,
                Key = region.Key ?? string.Empty,
                ShapeCount = 0,
                FactsJson = "{}"
            });

            ushort compiledShapeCount = 0;

            for (int i = 0; i < shapes.Length; i++)
            {
                BaseShape shape = shapes[i];
                if (shape == null || !shape.Enabled)
                    continue;

                if (!GeometryCompiler.TryCompile(
                        shape,
                        regionId,
                        ESpatialObjectType.Region,
                        dataset,
                        out SpatialProxy proxy))
                {
                    continue;
                }

                dataset.SpatialProxies.Add(proxy);
                compiledShapeCount++;
                if (compiledShapeCount == 0)
                    throw new System.OverflowException();
            }

            RegionRecord record = dataset.Regions[dataset.Regions.Count - 1];
            record.ShapeCount = compiledShapeCount;
            dataset.Regions[dataset.Regions.Count - 1] = record;

            return true;
        }

        private static BaseShape[] GetDirectShapes(Region region)
        {
            BaseShape[] found = region.GetComponentsInChildren<BaseShape>(true);
            int count = 0;

            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null && found[i].transform.parent == region.transform)
                    count++;
            }

            BaseShape[] result = new BaseShape[count];
            int index = 0;

            for (int i = 0; i < found.Length; i++)
            {
                BaseShape shape = found[i];
                if (shape != null && shape.transform.parent == region.transform)
                    result[index++] = shape;
            }

            return result;
        }
    }
}
#endif
