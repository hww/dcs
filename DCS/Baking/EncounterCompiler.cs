#if UNITY_EDITOR
using DCS.Core;
using DCS.Gameplay;
using UnityEngine;

namespace DCS.Core.Packing
{
    public static class EncounterCompiler
    {
        public static bool TryCompile(
            Encounter encounter,
            DCS.Gameplay.MapDataset dataset,
            ushort encounterId)
        {
            if (encounter == null || dataset == null)
                return false;

            dataset.Encounters.Add(new EncounterRecord
            {
                Id = encounterId,
                Key = encounter.Key ?? string.Empty,
                ScriptPath = encounter.ScriptPath ?? string.Empty,
                AutoStart = encounter.AutoStart,
                FactsJson = encounter.Facts != null ? encounter.Facts.ToJson() : "{}"
            });

            return true;
        }
    }
}
#endif
