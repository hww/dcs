using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DynamicComponent.Lua
{
    /// <summary>
    /// Core entry point of the Lua script engine backend.
    /// Manages the global execution runtime state and coordinates file reads.
    /// </summary>
    public class LuaManager : MonoBehaviour
    {
        private static LuaManager _instance;
        public static LuaManager Instance => _instance;

        private LuaStateWrapper _globalLuaState;

        // Explicit static references for P/Invoke callback thread safety
        private static HostChain _globalHostChain;
        private static EventSubscription _eventSubscriptionPool;
        private static TypeChain _globalTypeChain;

        private string LuaRootPath => Path.Combine(Application.streamingAssetsPath, "Lua").Replace("\\", "/");

        /// <summary>
        /// Binds the active game loop structural layout manager to the bridge context.
        /// Must be called before LuaManager initializes.
        /// </summary>
        public static void BindHostChain(HostChain hostChain)
        {
            _globalHostChain = hostChain;
        }

        /// <summary>
        /// Binds the active DCS event subscription management pipelines to the script scope.
        /// Must be called before LuaManager initializes.
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
                Debug.Log("[LuaManager] Starting global virtual machine instance...");
                _globalLuaState = new LuaStateWrapper("GlobalEngine");
                IntPtr L = _globalLuaState.L;

                // ============================================================
                // 1. META: Component type info
                // ============================================================
                RegisterLuaFunction(L, Lua_GetTypesCount, "DCS_Internal_GetTypesCount");
                RegisterLuaFunction(L, Lua_GetTypeNameById, "DCS_Internal_GetTypeNameById");

                // ============================================================
                // 2. COMPONENT OPERATIONS
                // ============================================================
                RegisterLuaFunction(L, Lua_CreateComponent, "DCS_CreateComponent");
                RegisterLuaFunction(L, Lua_RemoveComponent, "DCS_RemoveComponent");
                RegisterLuaFunction(L, Lua_HasComponent, "DCS_HasComponent");

                // ============================================================
                // 3. FIELD ACCESS — ЭТО ВСЁ, ЧТО НУЖНО ДЛЯ ЧТЕНИЯ ПОЛЕЙ
                // ============================================================
                RegisterLuaFunction(L, Lua_GetField, "DCS_GetField");
                RegisterLuaFunction(L, Lua_SetField, "DCS_SetField");

                // ============================================================
                // 4. EVENTS
                // ============================================================
                RegisterLuaFunction(L, Lua_EmitEvent, "DCS_EmitEvent");
                RegisterLuaFunction(L, Lua_RegisterSubscription, "DCS_RegisterSubscription");
                RegisterLuaFunction(L, Lua_DeliverEvent, "DCS_DeliverEvent");

                // ============================================================
                // 5. BOOTSTRAP
                // ============================================================
                string bootstrapPath = $"{LuaRootPath}/Core/bootstrap.lua";
                if (File.Exists(bootstrapPath))
                {
                    string bootstrapCode = File.ReadAllText(bootstrapPath);
                    _globalLuaState.ExecuteString(bootstrapCode, "bootstrap.lua");
                }
                else
                {
                    Debug.LogError($"[LuaManager] Bootstrap not found: {bootstrapPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LuaManager] Initialization failed: {e.Message}");
            }
        }

        private static void RegisterLuaFunction(IntPtr L, Func<IntPtr, int> fn, string name)
        {
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(fn);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_setglobal(L, name);
        }

        // ============================================================
        //  PUBLIC EVENT DELIVERY
        // ============================================================

        /// <summary>
        /// Delivers a DCS event to the global Lua event router.
        /// Called from C# EventSystem when an event needs to be processed in Lua.
        /// </summary>
        /// <param name="hostId">Host that owns the event receiver.</param>
        /// <param name="eventTypeId">Type ID of the event.</param>
        /// <param name="packedHandle">Packed Handle of the event component.</param>
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

        // ============================================================
        //  P/INVOKE CALLBACKS
        //  All callbacks receive IntPtr L (Lua state) and return int (number of return values).
        //  Args are read from Lua stack using lua_tointegerx, lua_tostring, etc.
        // ============================================================

        // ---------- Meta ----------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetTypesCount(IntPtr L)
        {
            LuaNative.lua_pushinteger(L, ComponentRegistry.GetTypesCount());
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetTypeNameById(IntPtr L)
        {
            int id = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            string name = ComponentRegistry.GetTypeNameById(id);
            LuaNative.lua_pushstring(L, name);
            return 1;
        }

        // ---------- Component Operations ----------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_CreateComponent(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int hostId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            // Validate host
            if (hostId < 0 || hostId >= HostManager.GlobalHosts.Length)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            ref HostData hostData = ref HostManager.GlobalHosts[hostId];
            Host host = new Host { Id = (ushort)hostId, Generation = hostData.Generation };

            // Validate type
            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            var pool = ComponentRegistry.Pools[typeId];
            if (pool != null && _globalHostChain != null)
            {
                Handle handle = pool.SystemAllocate(host, _globalHostChain);
                LuaNative.lua_pushinteger(L, handle.Pack());
                return 1;
            }

            LuaNative.lua_pushnil(L);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_RemoveComponent(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            // Validate handle
            if (packedHandle == HandleConfig.NULL_INDEX)
            {
                return 0;
            }

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];

            if (pool != null && _globalHostChain != null)
            {
                // Get host from roster
                if (pool.TryGetHost(handle, out Host host))
                {
                    pool.SystemFree(host, _globalHostChain, handle);
                }
            }

            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_HasComponent(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int hostId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            // Validate host
            if (hostId < 0 || hostId >= HostManager.GlobalHosts.Length)
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            ref HostData hostData = ref HostManager.GlobalHosts[hostId];
            Host host = new Host { Id = (ushort)hostId, Generation = hostData.Generation };

            bool has = false;
            if (_globalHostChain != null)
            {
                ChainNode node = _globalHostChain.GetTypedHandle(host, typeId);
                has = !node.IsNull;  // ChainNode.IsNull checks if Component is null
            }

            LuaNative.lua_pushboolean(L, has ? 1 : 0);
            return 1;
        }

        // ---------- Field Access (unified, pool-driven) ----------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetField(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            IntPtr ptr = LuaNative.lua_tolstring(L, 3, IntPtr.Zero);
            string fieldName = ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;

            if (packedHandle == HandleConfig.NULL_INDEX)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];

            // Теперь TryGetDenseIndex есть в IComponentPool!
            if (pool != null && pool.TryGetDenseIndex(handle, out int denseIndex))
            {
                pool.GetField(denseIndex, fieldName, L);
                return 1;
            }

            LuaNative.lua_pushnil(L);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_SetField(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            IntPtr ptr = LuaNative.lua_tolstring(L, 3, IntPtr.Zero);
            string fieldName = ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;

            if (packedHandle == HandleConfig.NULL_INDEX)
                return 0;

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];

            if (pool != null && pool.TryGetDenseIndex(handle, out int denseIndex))
            {
                pool.SetField(denseIndex, fieldName, L);
            }

            return 0;
        }

        // ---------- Events ----------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_EmitEvent(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int hostId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            // Validate host
            if (hostId < 0 || hostId >= HostManager.GlobalHosts.Length)
            {
                return 0;
            }

            ref HostData hostData = ref HostManager.GlobalHosts[hostId];
            Host host = new Host { Id = (ushort)hostId, Generation = hostData.Generation };

            var pool = ComponentRegistry.Pools[typeId];
            if (pool != null && _globalHostChain != null)
            {
                Handle handle = pool.SystemAllocate(host, _globalHostChain);
                if (!handle.IsNull)
                {
                    DeliverEventToLua(hostId, typeId, handle.Pack());
                }
            }

            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_RegisterSubscription(IntPtr L)
        {
            int hostId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int eventTypeId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            // Validate host
            if (hostId < 0 || hostId >= HostManager.GlobalHosts.Length)
            {
                return 0;
            }

            ref HostData hostData = ref HostManager.GlobalHosts[hostId];
            Host host = new Host { Id = (ushort)hostId, Generation = hostData.Generation };

            if (_eventSubscriptionPool != null && _globalHostChain != null && _globalTypeChain != null)
            {
                _eventSubscriptionPool.SystemSubscribe(host, eventTypeId, _globalHostChain, _globalTypeChain);
                Debug.Log($"[Lua] Subscribed Host {hostId} to Event {eventTypeId}");
            }

            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_DeliverEvent(IntPtr L)
        {
            int receiverHostId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int eventTypeId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 3, IntPtr.Zero);

            // Validate
            if (packedHandle == HandleConfig.NULL_INDEX || receiverHostId < 0)
            {
                return 0;
            }

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[eventTypeId];

            if (pool != null && pool.TryGetDenseIndex(handle, out int _))
            {
                // Deliver to Lua via event router
                DeliverEventToLua(receiverHostId, eventTypeId, packedHandle);
            }

            return 0;
        }

        // ---------- File Access ----------
        public string ReadScriptFile(string relativePath)
        {
            string fullPath = $"{LuaRootPath}/{relativePath}";
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[LuaManager] Script not found: {fullPath}");
                return string.Empty;
            }
            return File.ReadAllText(fullPath);
        }

        void OnDestroy()
        {
            _globalLuaState?.Dispose();
        }
    }
}