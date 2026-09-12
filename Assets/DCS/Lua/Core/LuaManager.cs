using System;
using System.IO;
using UnityEngine;
using DynamicComponent.Lua.Bindings; // Подключаем слой биндингов

namespace DynamicComponent.Lua
{
    public class LuaManager : MonoBehaviour
    {
        private static LuaManager _instance;
        public static LuaManager Instance => _instance;

        private LuaStateWrapper _globalLuaState;
        public IntPtr MainState => _globalLuaState != null ? _globalLuaState.L : IntPtr.Zero;

        private string LuaRootPath => Path.Combine(Application.streamingAssetsPath, "Lua").Replace("\\", "/");

        // Ограниченный внутренний доступ для маршалинга внутри папки Bindings
        internal static HostChain _globalHostChain;
        internal static EventSubscription _eventSubscriptionPool;
        internal static TypeChain _globalTypeChain;

        /// <summary>
        /// Binds the active game loop structural layout manager to the bridge context.
        /// </summary>
        public static void BindHostChain(HostChain hostChain)
        {
            _globalHostChain = hostChain;
        }

        /// <summary>
        /// Binds the active DCS event subscription management pipelines to the script scope.
        /// </summary>
        public static void BindEventSystems(EventSubscription subPool, TypeChain typeChain)
        {
            _eventSubscriptionPool = subPool;
            _globalTypeChain = typeChain;
        }

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

        private void Start()
        {
            InitializeGlobalEngine();
        }

        private void InitializeGlobalEngine()
        {
            try
            {
                Debug.Log("[LuaManager] Регистрация глобальной виртуальной машины...");
                _globalLuaState = new LuaStateWrapper("GlobalEngine");
                IntPtr L = _globalLuaState.L;

                // --- КОНВЕЙЕР РЕГИСТРАЦИИ БИНДИНГОВ ---
                EcsBindings.Register(L);
                EventBindings.Register(L);
                SpatialBindings.Register(L);
                MapBindings.Register(L);

                // Загрузка базовой экосистемы скриптов
                string bootstrapPath = $"{LuaRootPath}/Core/bootstrap.lua";
                if (File.Exists(bootstrapPath))
                {
                    string bootstrapCode = File.ReadAllText(bootstrapPath);
                    _globalLuaState.ExecuteString(bootstrapCode, "bootstrap.lua");
                }
                else
                {
                    Debug.LogError($"[LuaManager] Bootstrap-файл не найден: {bootstrapPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LuaManager] Ошибка старта скриптового ядра: {e.Message}");
            }
        }

        /// <summary>
        /// Delivers a DCS event to the global Lua event router.
        /// </summary>
        public static void DeliverEventToLua(int hostId, int eventTypeId, int packedHandle)
        {
            if (_instance == null || _instance._globalLuaState == null) return;

            IntPtr L = _instance._globalLuaState.L;
            LuaNative.lua_getglobal(L, "DCS_Global_EventRouter");

            if (LuaNative.lua_type(L, -1) == LuaNative.LUA_TFUNCTION)
            {
                LuaNative.lua_pushinteger(L, hostId);
                LuaNative.lua_pushinteger(L, eventTypeId);
                LuaNative.lua_pushinteger(L, packedHandle);

                if (LuaNative.lua_pcallk(L, 3, 0, 0, 0, IntPtr.Zero) != 0)
                {
                    string error = _instance._globalLuaState.GetStringFromStack(-1);
                    Debug.LogError($"[Lua] Event router error: {error}");
                }
            }
        }

        public string ReadScriptFile(string relativePath)
        {
            string fullPath = $"{LuaRootPath}/{relativePath}";
            return File.Exists(fullPath) ? File.ReadAllText(fullPath) : string.Empty;
        }

        void OnDestroy()
        {
            _globalLuaState?.Dispose();
        }
    }
}
