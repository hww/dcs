using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Системный сервис связки Host ↔ GameObject.
    /// Знает только Host, HostManager, IHostReference.
    /// </summary>
    public static class ViewService
    {
        /// <summary>
        /// Связать уже существующий GameObject (по имени в сцене) с хостом.
        /// </summary>
        public static bool Attach(Host host, string sceneObjectName)
        {
            if (!HostManager.IsValid(host))
            {
                Debug.LogError($"[ViewService] Invalid host: {host}");
                return false;
            }
            if (string.IsNullOrEmpty(sceneObjectName))
            {
                Debug.LogError("[ViewService] Empty scene object name");
                return false;
            }

            GameObject go = GameObject.Find(sceneObjectName);
            if (go == null)
            {
                Debug.LogError($"[ViewService] Scene object not found: {sceneObjectName}");
                return false;
            }

            return AttachToGameObject(host, go);
        }

        /// <summary>
        /// Создать GameObject из префаба и связать с хостом.
        /// </summary>
        public static bool Spawn(Host host, string prefabPath,
                                 Vector3 position = default,
                                 Quaternion rotation = default)
        {
            if (!HostManager.IsValid(host))
            {
                Debug.LogError($"[ViewService] Invalid host: {host}");
                return false;
            }
            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError("[ViewService] Empty prefab path");
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
            return AttachToGameObject(host, go);
        }

        // ------------------------------------------------------------
        //  Общая связка Host ↔ GameObject
        // ------------------------------------------------------------
        private static bool AttachToGameObject(Host host, GameObject go)
        {
            var link = go.GetComponent<IHostReference>();
            if (link == null)
            {
                Debug.LogError($"[ViewService] No IHostReference on {go.name}");
                Object.Destroy(go);
                return false;
            }

            HostManager.LinkHostReference(host, link);
            return true;
        }
    }
}