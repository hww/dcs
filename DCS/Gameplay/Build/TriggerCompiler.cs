#if UNITY_EDITOR
using DCS.Gameplay;
using DCS.Gameplay.Build;
using DCS.Interaction.Authoring;
using DCS.Spatial;


namespace DCS.Core.Packing
{
    public static class TriggerCompiler
    {
        public static bool TryCompile(
            Trigger trigger,
            MapDataset dataset,
            ushort triggerId,
            ushort encounterId)
        {
            if (trigger == null || dataset == null || !trigger.Enabled)
                return false;

            BaseShape[] shapes = trigger.GetShapes();

            dataset.Triggers.Add(new TriggerRecord
            {
                Id = triggerId,
                EncounterId = encounterId,
                Key = trigger.Key ?? string.Empty,
                Mode = (byte)trigger.Mode,
                OneShot = trigger.OneShot,
                ShapeCount = 0
            });

            ushort compiledShapeCount = 0;

            for (int i = 0; i < shapes.Length; i++)
            {
                BaseShape shape = shapes[i];
                if (shape == null || !shape.Enabled)
                    continue;

                if (!GeometryCompiler.TryCompile(
                        shape,
                        triggerId,
                        ESpatialObjectType.Trigger,
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

            TriggerRecord record = dataset.Triggers[dataset.Triggers.Count - 1];
            record.ShapeCount = compiledShapeCount;
            dataset.Triggers[dataset.Triggers.Count - 1] = record;

            return true;
        }
    }
}
#endif
