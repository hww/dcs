// DCS.Core.ResourceLoader
using UnityEngine;

namespace DCS.Core
{
    public static class ResourceLoader
    {
        /// <summary>
        /// Загружает GameObject-префаб из Resources.
        /// Возвращает null, если не найден.
        /// </summary>
        public static GameObject LoadPrefab(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            return Resources.Load<GameObject>(path);
        }

        /// <summary>
        /// Инстанциирует префаб.
        /// </summary>
        public static GameObject Instantiate(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            if (prefab == null) return null;
            return Object.Instantiate(prefab, pos, rot);
        }
    }
}