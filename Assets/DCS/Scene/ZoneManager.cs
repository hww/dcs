using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DynamicComponent.Lua
{
    /// <summary>
    /// Global manager responsible for asynchronous scene streaming, local scope allocation,
    /// and routing world entities from Unity subscenes straight into the script engine layer.
    /// </summary>
    public class ZoneManager : MonoBehaviour
    {
        private static ZoneManager _instance;
        public static ZoneManager Instance => _instance;

        // Tracks all active actors currently loaded in the world registry by their unique names
        private readonly Dictionary<string, EntityActor> _activeActors = new Dictionary<string, EntityActor>(StringComparer.OrdinalIgnoreCase);

        // Tracks active local Lua contexts by their scene location folder names
        private readonly Dictionary<string, ZoneScriptContext> _activeZones = new Dictionary<string, ZoneScriptContext>(StringComparer.OrdinalIgnoreCase);

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Asynchronously streams a gameplay location zone and spawns its respective Lua context script.
        /// </summary>
        /// <param name="sceneName">The exact name of the Unity asset scene to load additively.</param>
        /// <param name="luaScriptRelativePath">Relative path to the zone director script file from Lua root directory.</param>
        public void LoadZoneAsync(string sceneName, string luaScriptRelativePath)
        {
            if (_activeZones.ContainsKey(sceneName))
            {
                Debug.LogWarning($"[ZoneManager] Zone scene '{sceneName}' is already loaded or pending initialization.");
                return;
            }

            // Trigger Unity's native multiplatform asynchronous scene streaming thread pass
            var asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            asyncLoad.completed += (operation) =>
            {
                InitializeLoadedZone(sceneName, luaScriptRelativePath);
            };
        }

        private void InitializeLoadedZone(string sceneName, string luaScriptRelativePath)
        {
            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (!loadedScene.IsValid()) return;

            Debug.Log($"<color=orange>[ZoneManager]</color> Scene '{sceneName}' streamed successfully. Discovering actors...");

            // Step 1: Scan the newly loaded subscene hierarchy for all passive Actor anchors
            GameObject[] rootObjects = loadedScene.GetRootGameObjects();
            List<EntityActor> zoneActors = new List<EntityActor>();

            foreach (var root in rootObjects)
            {
                var actorsInRoot = root.GetComponentsInChildren<EntityActor>(true);
                zoneActors.AddRange(actorsInRoot);
            }

            // Step 2: Register discovered scene entities inside our flat layout map cache
            foreach (var actor in zoneActors)
            {
                if (!_activeActors.ContainsKey(actor.DisplayName))
                {
                    _activeActors[actor.DisplayName] = actor;
                }
                else
                {
                    Debug.LogError($"[ZoneManager] Duplicate unique name clash detected on scene load! Name: '{actor.DisplayName}'");
                }
            }

            // Step 3: Spin up an isolated Local Lua Context State for this specific streamed geography zone
            Func<string, BaseFacts> registryLookupDelegate = (entityName) =>
            {
                if (_activeActors.TryGetValue(entityName, out EntityActor actor))
                {
                    return actor.Facts;
                }
                return null;
            };

            var zoneContext = new ZoneScriptContext(sceneName, registryLookupDelegate);
            _activeZones[sceneName] = zoneContext;

            // Step 4: Read the target level script from StreamingAssets and fire the director execution thread
            string scriptCode = LuaManager.Instance.ReadScriptFile(luaScriptRelativePath);
            if (!string.IsNullOrEmpty(scriptCode))
            {
                zoneContext.RunDirectorScript(scriptCode);
            }
        }

        /// <summary>
        /// Completely unloads a gameplay zone scene and cleans its local isolated Lua memory scope blocks.
        /// </summary>
        public void UnloadZoneAsync(string sceneName)
        {
            if (_activeZones.TryGetValue(sceneName, out ZoneScriptContext context))
            {
                Debug.Log($"<color=orange>[ZoneManager]</color> Unloading zone '{sceneName}'. Stripping local scope references.");

                context.Dispose();
                _activeZones.Remove(sceneName);

                Scene sceneToUnload = SceneManager.GetSceneByName(sceneName);
                if (sceneToUnload.IsValid())
                {
                    foreach (var root in sceneToUnload.GetRootGameObjects())
                    {
                        foreach (var actor in root.GetComponentsInChildren<EntityActor>(true))
                        {
                            _activeActors.Remove(actor.DisplayName);
                        }
                    }
                    SceneManager.UnloadSceneAsync(sceneToUnload);
                }
            }
        }
    }
}
