using System;
using System.IO;
using UnityEngine;
using DCS.Core;
using DCS.Lua.Bindings;

namespace DCS.Lua
{
    public class LuaManager : MonoBehaviour
    {
        private static LuaManager _instance;
        public static LuaManager Instance => _instance;

        private LuaStateWrapper _globalLuaState;
        public IntPtr MainState => _globalLuaState != null ? _globalLuaState.L : IntPtr.Zero;

        private string LuaRootPath =>
            Path.Combine(Application.streamingAssetsPath, "Lua").Replace("\\", "/");

        public static HostChain _globalHostChain;
        public static EventSubscription _eventSubscriptionPool;
        public static TypeChain _globalTypeChain;

        /// <summary>
        /// Делегат регистрации биндингов. Игра назначает свой.
        /// Если не назначен — используется базовый LuaBindings.RegisterAll.
        /// </summary>
        public static System.Action<IntPtr> RegisterBindingsCallback;

        public static void BindHostChain(HostChain hostChain)
        {
            _globalHostChain = hostChain;
        }

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
            ComponentRegistry.InitializeAllPools();
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

                // System bindings
                LuaBindings.RegisterAll(L);
                
                // The game bindings
                if (RegisterBindingsCallback != null)
                    RegisterBindingsCallback(L);

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

        // ------------------------------------------------------------
        //  NEW: per-frame Lua tick. Calls DCS_Global_FrameUpdate()
        //  which iterates LuaEntitiesRegistry and calls entity:Update().
        // ------------------------------------------------------------
        void Update()
        {
            if (_globalLuaState == null) return;

            IntPtr L = _globalLuaState.L;

            LuaNative.lua_getglobal(L, "DCS_Global_FrameUpdate");
            if (LuaNative.lua_type(L, -1) == LuaNative.LUA_TFUNCTION)
            {
                if (LuaNative.lua_pcallk(L, 0, 0, 0, 0, IntPtr.Zero) != 0)
                {
                    string error = _globalLuaState.GetStringFromStack(-1);
                    Debug.LogError($"[Lua] FrameUpdate error: {error}");
                    LuaNative.lua_settop(L, -2);
                }
            }
            else
            {
                // Not a function (or nil) — pop it to keep the stack clean.
                LuaNative.lua_settop(L, -2);
            }
        }

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
                    LuaNative.lua_settop(L, -2);
                }
            }
            else
            {
                LuaNative.lua_settop(L, -2);
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