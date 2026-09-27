using UnityEngine;

namespace DCS.World
{
    /// <summary>
    /// Минимальный MonoBehaviour для запуска корутин стриминга сцен
    /// из статических методов (SceneBindings).
    /// Создаётся лениво при первом обращении к Instance.
    /// </summary>
    public sealed class SceneStreamRunner : MonoBehaviour
    {
        private static SceneStreamRunner _instance;

        public static SceneStreamRunner Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("[SceneStreamRunner]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<SceneStreamRunner>();
                return _instance;
            }
        }
    }
}