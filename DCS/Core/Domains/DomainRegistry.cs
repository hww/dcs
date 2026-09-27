using System.Collections.Generic;

namespace DCS.Core
{
    public static class DomainRegistry
    {
        private static readonly List<Domain> _byId = new();
        private static readonly Dictionary<string, int> _byName = new();

        public static Domain Create(string name, int hostCapacity = 50000, int subCapacity = 1000)
        {
            if (string.IsNullOrEmpty(name))
                name = $"domain_{_byId.Count}";

            if (_byName.TryGetValue(name, out int existing))
                return _byId[existing];

            int id = _byId.Count;
            var domain = new Domain(id, name, hostCapacity, subCapacity);
            _byId.Add(domain);
            _byName[name] = id;
            return domain;
        }

        public static Domain Get(int id)
        {
            if (id < 0 || id >= _byId.Count)
                return null;
            return _byId[id];
        }

        public static Domain Get(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            return _byName.TryGetValue(name, out int id) ? _byId[id] : null;
        }

        /// <summary>
        /// Ensures the default domain exists. Called once at engine startup.
        /// </summary>
        public static Domain EnsureDefault()
        {
            if (_byId.Count == Domain.DefaultId)
                Create("default");
            return _byId[Domain.DefaultId];
        }

        public static int Count => _byId.Count;

        public static void Clear()
        {
            _byId.Clear();
            _byName.Clear();
        }
    }
}