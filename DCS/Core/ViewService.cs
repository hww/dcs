// DCS.Core.ViewService
using UnityEngine;

namespace DCS.Core
{
    public static class ViewService
    {
        /// <summary>
        /// Связывает Host с GameObject, инстанциированным из префаба.
        /// Возвращает true, если префаб найден и GameObject связан с хостом.
        /// </summary>
        public static bool AttachPrefab(Host host, string prefabPath,
                                        Vector3 position = default,
                                        Quaternion rotation = default)
        {
            if (!HostManager.IsValid(host))
            {
                Debug.LogError($"[ViewService] Invalid host: {host}");
                return false;
            }

            GameObject prefab = ResourceLoader.LoadPrefab(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[ViewService] Prefab not found: {prefabPath}");
                return false;
            }

            GameObject go = ResourceLoader.Instantiate(prefab, position, rotation);
            if (go == null)
            {
                Debug.LogError($"[ViewService] Failed to instantiate {prefabPath}");
                return false;
            }

            go.name = $"Host_{host.Id}_{prefab.name}";

            // Связка Host ↔ MonoBehaviour-референс
            var link = go.GetComponent<IHostReference>();
            if (link == null)
            {
                Debug.LogError($"[ViewService] No IHostReference on prefab {prefabPath}");
                Object.Destroy(go);
                return false;
            }

            HostManager.LinkHostReference(host, link);
            return true;
        }
    }
}