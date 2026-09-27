using DCS.Core;
using System;
using System.IO;
using UnityEngine;

namespace DCS.Lua
{
    /// <summary>
    /// Lua-машина. Не MonoBehaviour, не синглтон.
    /// Создаётся Root-ом игры и живёт как обычная подсистема.
    ///
    /// Root вызывает:
    ///   Initialize(config) — один раз на старте
    ///   Update()           — каждый кадр
    ///   Deinitialize()     — при выключении
    /// </summary>
    public sealed class LuaManager
    {   
        /// <summary>
        /// Текущий активный LuaManager. Используется биндингами, у которых
        /// нет ссылки на Root. Не синглтон — ставится в Initialize, чистится в Deinitialize.
        /// </summary>
        public static LuaManager Current { get; private set; }
        private LuaStateWrapper _globalLuaState;
        private LuaTcpServer _replServer;
        private string _luaRootPath;
        private bool _initialized;

        /// <summary>Указатель на Lua state. IntPtr.Zero, если не инициализирован.</summary>
        public IntPtr MainState => _globalLuaState != null ? _globalLuaState.L : IntPtr.Zero;

        /// <summary>
        /// Внешний callback для регистрации игровых биндингов.
        /// Игра выставляет его до вызова Initialize.
        /// </summary>
        public static Action<IntPtr> RegisterBindingsCallback;

        public void Initialize(LuaManagerConfig config)
        {
            if (_initialized) return;
            _initialized = true;
            _luaRootPath = config.LuaRootPath;
            Current = this;

            ComponentRegistry.InitializeAllPools();
            DomainRegistry.EnsureDefault();

            try
            {
                Debug.Log("[LuaManager] Registering global VM...");
                _globalLuaState = new LuaStateWrapper("GlobalEngine");
                IntPtr L = _globalLuaState.L;

                LuaBindings.RegisterAll(L);
                RegisterBindingsCallback?.Invoke(L);

                string bootstrapPath = $"{_luaRootPath}/Core/bootstrap.lua";
                if (File.Exists(bootstrapPath))
                {
                    string bootstrapCode = File.ReadAllText(bootstrapPath);
                    _globalLuaState.ExecuteString(bootstrapCode, "bootstrap.lua");
                }
                else
                {
                    Debug.LogError($"[LuaManager] Bootstrap file not found: {bootstrapPath}");
                }

                if (config.EnableRepl)
                {
                    _replServer = new LuaTcpServer(L, config.ReplPort);
                    _replServer.StartServer();
                }

                Debug.Log("[LuaManager] Core initialized.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LuaManager] Failed to start script engine:\n{e}");
            }
        }

        /// <summary>Тик Lua. Вызывается Root-ом каждый кадр.</summary>
        public void Update()
        {
            if (_globalLuaState == null) return;
            IntPtr L = _globalLuaState.L;

            _replServer?.Tick();

            int topBefore = LuaNative.lua_gettop(L);
            LuaNative.lua_getglobal(L, "DCS_Global_FrameUpdate");
            if (LuaNative.lua_type(L, -1) == LuaNative.LUA_TFUNCTION)
                _globalLuaState.ProtectedCall(0, 0);
            else
                LuaNative.lua_settop(L, topBefore);
        }

        public void Deinitialize()
        {
            if (!_initialized) return;
            if (Current == this) Current = null;
            _replServer?.StopServer();
            _replServer = null;
            _globalLuaState?.Dispose();
            _globalLuaState = null;
            _initialized = false;
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

        /// <summary>
        /// Вызов Lua-роутера событий.
        /// Используется из EventSystem.DeliverAll(...) через делегат.
        /// </summary>
        public void CallEventRouter(int senderHostId, int eventTypeId, int packedHandle)
        {
            if (_globalLuaState == null) return;
            IntPtr L = _globalLuaState.L;

            int topBefore = LuaNative.lua_gettop(L);
            LuaNative.lua_getglobal(L, "DCS_Global_EventRouter");
            if (LuaNative.lua_type(L, -1) != LuaNative.LUA_TFUNCTION)
            {
                LuaNative.lua_settop(L, topBefore);
                return;
            }

            LuaNative.lua_pushinteger(L, senderHostId);
            LuaNative.lua_pushinteger(L, eventTypeId);
            LuaNative.lua_pushinteger(L, packedHandle);
            _globalLuaState.ProtectedCall(3, 0);
        }

        public string ReadScriptFile(string relativePath)
        {
            if (string.IsNullOrEmpty(_luaRootPath)) return string.Empty;
            string fullPath = $"{_luaRootPath}/{relativePath}";
            return File.Exists(fullPath) ? File.ReadAllText(fullPath) : string.Empty;
        }
    }
}