using UnityEngine;

namespace DCS.Core
{
    public static class ViewService
    {
        /// <summary>
        /// Привязывает существующий GameObject к Host.
        /// Порядок: LinkHostReference → SetContext → Birth.
        /// </summary>
        public static bool Attach(Host host, string sceneObjectName, object context = null)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Debug.Log($"[ViewService] Attach '{sceneObjectName}' frame={Time.frameCount} " +
                      $"scene='{scene.name}' loaded={scene.isLoaded}");

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
                Debug.LogError($"[ViewService] Scene object not found: `{sceneObjectName}`");
                return false;
            }
            return AttachToGameObject(host, go, context);
        }

        /// <summary>
        /// Инстанцирует префаб и привязывает к Host.
        /// Порядок: Instantiate → LinkHostReference → SetContext → Birth.
        /// </summary>
        public static bool Spawn(
            Host host,
            string prefabPath,
            Vector3 position = default,
            Quaternion rotation = default,
            object context = null)
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
            return AttachToGameObject(host, go, context);
        }

        /// <summary>
        /// Общая точка привязки.
        /// Выполняет LinkHostReference → SetContext → Birth.
        /// </summary>
        private static bool AttachToGameObject(Host host, GameObject go, object context)
        {
            var link = go.GetComponent<IHostReference>();
            if (link == null)
            {
                Debug.LogError($"[ViewService] No IHostReference on {go.name}");
                Object.Destroy(go);
                return false;
            }

            // 1. Низкоуровневая привязка Host ↔ MonoBehaviour
            HostManager.LinkHostReference(host, link);

            // 2. Injection (опционально)
            if (context != null && link is IBirthContext ctx)
            {
                try { ctx.SetContext(context); }
                catch (System.Exception e)
                {
                    Debug.LogError(
                        $"[ViewService] SetContext failed on {go.name}: {e}");
                }
            }

            // 3. Игровая инициализация (кэши, регистрации)
            if (link is ILifeCycle lc)
            {
                try { lc.Birth(); }
                catch (System.Exception e)
                {
                    Debug.LogError(
                        $"[ViewService] Birth failed on {go.name}: {e}");
                }
            }

            return true;
        }
    }
}