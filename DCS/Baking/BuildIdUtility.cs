using System.Collections.Generic;
using UnityEngine;
namespace DCS.Gameplay.Build
{
    public static class BuildIdUtility
    {
        public static ushort GetId(Component component, HashSet<ushort> used)
        {
            string path = GetPath(component.transform); uint hash = 2166136261u;
            for (int i = 0; i < path.Length; i++) { hash ^= path[i]; hash *= 16777619u; }
            ushort id = (ushort)(hash & 0xFFFE); if (id == 0) id = 1;
            while (used.Contains(id)) { id++; if (id == 0) id = 1; }
            used.Add(id); return id;
        }
        private static string GetPath(Transform t)
        {
            List<string> p = new List<string>(); while (t != null) { p.Add(t.name); t = t.parent; }
            p.Reverse(); return string.Join("/", p.ToArray());
        }
    }
}
