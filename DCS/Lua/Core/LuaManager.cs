using DCS.Core;
using DCS.Lua.Bindings;
using System;
using System.IO;
using UnityEngine;

namespace DCS.Lua
{
    /// <summary>
    /// Global Lua VM. Self-creating singleton: first access to Instance
    /// builds the GameObject and runs full initialization synchronously.
    /// </summary>
    public sealed class LuaManager : MonoBehaviour
    {
        private static LuaManager _instance;

        public static LuaManager Instance
        {
            get
            {
                if (_instance != null) return _instance;

                var go = new GameObject("[LuaManager]");
                _instance = go.AddComponent<LuaManager>();
                _instance.Initialize();
                return _instance;
            }
        }

        private LuaStateWrapper _globalLuaState;
        public IntPtr MainState => _globalLuaState != null ? _globalLuaState.L : IntPtr.Zero;

        private string LuaRootPath =>
            Path.Combine(Application.streamingAssetsPath, "Lua").Replace("\\", "/");

        private LuaTcpServer _replServer;
        public static Action<IntPtr> RegisterBindingsCallback;

        private bool _initialized;

        // No Awake, no Start. Everything happens in Initialize().

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            Application.runInBackground = true;
            DontDestroyOnLoad(gameObject);

            ComponentRegistry.InitializeAllPools();
            DomainRegistry.EnsureDefault();

            try
            {
                Debug.Log("[LuaManager] Registering global VM...");
                _globalLuaState = new LuaStateWrapper("GlobalEngine");
                IntPtr L = _globalLuaState.L;

                LuaBindings.RegisterAll(L);
                RegisterBindingsCallback?.Invoke(L);

                string bootstrapPath = $"{LuaRootPath}/Core/bootstrap.lua";
                if (File.Exists(bootstrapPath))
                {
                    string bootstrapCode = File.ReadAllText(bootstrapPath);
                    _globalLuaState.ExecuteString(bootstrapCode, "bootstrap.lua");
                }
                else
                {
                    Debug.LogError($"[LuaManager] Bootstrap file not found: {bootstrapPath}");
                }

                _replServer = new LuaTcpServer(L, 49155);
                _replServer.StartServer();

                Debug.Log("[LuaManager] Core and nREPL initialized.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LuaManager] Failed to start script engine:\n{e}");
            }
        }

        void Update()
        {
            if (_globalLuaState == null) return;
            IntPtr L = _globalLuaState.L;

            _replServer?.Tick();

            int topBefore = LuaNative.lua_gettop(L);
            LuaNative.lua_getglobal(L, "DCS_Global_FrameUpdate");
            if (LuaNative.lua_type(L, -1) == LuaNative.LUA_TFUNCTION)
            {
                _globalLuaState.ProtectedCall(0, 0);
            }
            else
            {
                LuaNative.lua_settop(L, topBefore);
            }
        }

        public void CallGlobal(string functionName)
        {
            if (_globalLuaState == null || string.IsNullOrEmpty(functionName)) return;
            IntPtr L = _globalLuaState.L;

            int topBefore = LuaNative.lua_gettop(L);
            LuaNative.lua_getglobal(L, functionName);
            if (LuaNative.lua_type(L, -1) != LuaNative.LUA_TFUNCTION)
            {
                LuaNative.lua_settop(L, topBefore);
                return;
            }
            _globalLuaState.ProtectedCall(0, 0);
        }

        public void CallGlobal(string functionName, int arg)
        {
            if (_globalLuaState == null || string.IsNullOrEmpty(functionName)) return;
            IntPtr L = _globalLuaState.L;

            int topBefore = LuaNative.lua_gettop(L);
            LuaNative.lua_getglobal(L, functionName);
            if (LuaNative.lua_type(L, -1) != LuaNative.LUA_TFUNCTION)
            {
                LuaNative.lua_settop(L, topBefore);
                return;
            }
            LuaNative.lua_pushinteger(L, arg);
            _globalLuaState.ProtectedCall(1, 0);
        }

        public static void DeliverEventToLua(int hostId, int eventTypeId, int packedHandle)
        {
            if (_instance == null || _instance._globalLuaState == null) return;
            IntPtr L = _instance._globalLuaState.L;

            int topBefore = LuaNative.lua_gettop(L);
            LuaNative.lua_getglobal(L, "DCS_Global_EventRouter");
            if (LuaNative.lua_type(L, -1) != LuaNative.LUA_TFUNCTION)
            {
                LuaNative.lua_settop(L, topBefore);
                return;
            }
            LuaNative.lua_pushinteger(L, hostId);
            LuaNative.lua_pushinteger(L, eventTypeId);
            LuaNative.lua_pushinteger(L, packedHandle);
            _instance._globalLuaState.ProtectedCall(3, 0);
        }

        public string ReadScriptFile(string relativePath)
        {
            string fullPath = $"{LuaRootPath}/{relativePath}";
            return File.Exists(fullPath) ? File.ReadAllText(fullPath) : string.Empty;
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
            _replServer?.StopServer();
            _globalLuaState?.Dispose();
        }
    }
}