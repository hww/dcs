using System.Collections.Generic;

namespace DCS.World
{
    /// <summary>
    /// Optional O(1) lookup by name.
    /// This is a derived index — it does NOT own identity. Host owns identity.
    /// Must be kept in sync by a system when NameComponent is added/removed/changed.
    /// </summary>
    public sealed class NameIndex
    {
        private readonly Dictionary<string, ushort> _byName = new(1024);

        public void Add(string name, ushort hostId)
        {
            if (string.IsNullOrEmpty(name)) return;
            _byName[name] = hostId;
        }

        public void Remove(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            _byName.Remove(name);
        }

        public bool TryGet(string name, out ushort hostId)
        {
            return _byName.TryGetValue(name, out hostId);
        }

        public void Clear() => _byName.Clear();
        public int Count => _byName.Count;
    }
}