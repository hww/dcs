using System.Collections.Generic;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Optional O(1) lookup by name.
    /// This is a derived index — it does NOT own identity. Host owns identity.
    /// Must be kept in sync by a system when NameComponent is added/removed/changed.
    /// </summary>
    public sealed class NameIndex
    {
        private readonly Dictionary<uint, ushort> _byName = new(1024);

        public void Add(Name name, ushort hostId)
        {
            if (_byName.TryGetValue(name.Id, out ushort aHostId))
            {
                if (aHostId != hostId)
                {
                    return;
                } else
                {
                    Debug.Log($"[NameIndex] The name `{name.ToString()}` is already used.");
                    return;
                }
            }

            _byName[name.Id] = hostId;
        }

        public void Remove(Name name)
        {
            _byName.Remove(name.Id);
        }

        public bool TryGet(Name name, out ushort hostId)
        {
            return _byName.TryGetValue(name.Id, out hostId);
        }


        public void Clear() => _byName.Clear();
        public int Count => _byName.Count;
    }
}