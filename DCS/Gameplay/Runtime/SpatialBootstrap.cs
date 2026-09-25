using UnityEngine;
using DCS.Core;
using DCS.Lua.Bindings;

namespace DCS.Spatial
{
    /// <summary>
    /// Глобальный владелец ActorRegistry и DynamicSpatial.
    /// Ставится на один GameObject в первой сцене.
    /// </summary>
    public sealed class SpatialBootstrap : MonoBehaviour
    {
        public static SpatialBootstrap Instance { get; private set; }

        [Header("Grid Configuration")]
        [SerializeField] private int _gridWidth = 16;
        [SerializeField] private int _gridHeight = 16;
        [SerializeField] private int _gridDepth = 16;
        [SerializeField] private float _cellSize = 12.5f;

        public ActorRegistry Registry { get; private set; }
        public DynamicSpatial Spatial { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Registry = new ActorRegistry();
            Spatial = new DynamicSpatial(_gridWidth, _gridHeight, _gridDepth, _cellSize);

            // Регистрируем реестр для Lua-биндингов.
            ActorRegistryHolder.Set(Registry);

            Debug.Log($"[SpatialBootstrap] Grid {_gridWidth}x{_gridHeight}x{_gridDepth}, " +
                      $"cell={_cellSize}m, cells={_gridWidth * _gridHeight * _gridDepth}");
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}