using System.Collections.Generic;

namespace DCS.Gameplay
{
    public sealed class EncounterRuntimeRegistry
    {
        private readonly Dictionary<ushort, EncounterRuntime> _byId = new Dictionary<ushort, EncounterRuntime>();
        private readonly Dictionary<string, ushort> _idByKey = new Dictionary<string, ushort>();

        public void Load(IReadOnlyList<EncounterRecord> records)
        {
            Clear();
            if (records == null)
                return;

            for (int i = 0; i < records.Count; i++)
            {
                EncounterRuntime runtime = new EncounterRuntime(records[i]);
                _byId.Add(runtime.Id, runtime);
                if (!string.IsNullOrEmpty(runtime.Key) && !_idByKey.ContainsKey(runtime.Key))
                    _idByKey.Add(runtime.Key, runtime.Id);
            }
        }

        public void Clear()
        {
            _byId.Clear();
            _idByKey.Clear();
        }

        public bool TryGet(ushort id, out EncounterRuntime encounter) => _byId.TryGetValue(id, out encounter);

        public bool TryGet(string key, out EncounterRuntime encounter)
        {
            encounter = null;
            if (string.IsNullOrEmpty(key) || !_idByKey.TryGetValue(key, out ushort id))
                return false;

            return _byId.TryGetValue(id, out encounter);
        }

        public bool Activate(ushort id) => _byId.TryGetValue(id, out EncounterRuntime encounter) && encounter.Activate();
    }
}
