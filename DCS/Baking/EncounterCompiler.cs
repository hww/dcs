#if UNITY_EDITOR
using DCS.Authoring;
using DCS.Data;

namespace DCS.Baking
{
    public static class EncounterCompiler
    {
        public static bool TryCompile(
            Encounter encounter,
            MapDataset dataset,
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
