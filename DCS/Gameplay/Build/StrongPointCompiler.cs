#if UNITY_EDITOR
using DCS.Core;
using DCS.Gameplay;
using UnityEngine;

namespace DCS.Core.Packing
{
    public static class StrongPointCompiler
    {
        public static bool TryCompile(
            StrongPoint strongPoint,
            MapDataset dataset,
            ushort strongPointId,
            ushort encounterId)
        {
            if (strongPoint == null || dataset == null)
                return false;

            string tagsJson = "[]";
            if (strongPoint.Tags != null && strongPoint.Tags.Length > 0)
                tagsJson = JsonUtility.ToJson(new StringArrayWrapper { Values = strongPoint.Tags });

            dataset.StrongPoints.Add(new StrongPointRecord
            {
                Id = strongPointId,
                EncounterId = encounterId,
                Key = strongPoint.Key ?? string.Empty,
                Position = strongPoint.Position,
                Rotation = strongPoint.Rotation,
                Priority = strongPoint.Priority,
                TagsJson = tagsJson
            });

            return true;
        }

        [System.Serializable]
        private struct StringArrayWrapper
        {
            public string[] Values;
        }
    }
}
#endif
