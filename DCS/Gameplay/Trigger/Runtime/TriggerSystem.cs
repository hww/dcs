using System.Collections.Generic;
using UnityEngine;
using DCS.Spatial;

namespace DCS.Gameplay
{
    public sealed class TriggerSystem
    {
        private readonly List<TriggerRuntime> _triggers =
            new List<TriggerRuntime>();

        public int Count => _triggers.Count;

        public void Load(IReadOnlyList<TriggerRecord> records)
        {
            _triggers.Clear();

            if (records == null)
                return;

            for (int i = 0; i < records.Count; i++)
            {
                _triggers.Add(
                    new TriggerRuntime(records[i]));
            }
        }

        public void Clear()
        {
            _triggers.Clear();
        }

        public void Update(
            Vector3 observerPosition,
            SpatialRuntime spatial,
            float deltaTime)
        {
            for (int i = 0; i < _triggers.Count; i++)
            {
                _triggers[i].Evaluate(
                    observerPosition,
                    spatial,
                    deltaTime);
            }
        }
    }
}