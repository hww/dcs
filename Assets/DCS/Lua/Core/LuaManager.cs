using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DynamicComponent.Lua
{
    public class LuaManager : MonoBehaviour
    {
        private static LuaManager _instance;
        public static LuaManager Instance => _instance;

        private LuaStateWrapper _globalLuaState;

        // CRITICAL FIX: Explicit static reference for P/Invoke runtime safety
        private static HostChain _globalHostChain;

        private string LuaRootPath => Path.Combine(Application.streamingAssetsPath, "Lua").Replace("\\", "/");

        // Public setter to bind the active game loop HostChain to the Lua infrastructure
        public static void BindHostChain(HostChain hostChain)
        {
            _globalHostChain = hostChain;
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
            // Ensure pools are fully initialized in your architecture before spinning up the state
            InitializeGlobalEngine();

            // Симулируем, что C# EventSystem зафиксировала урон по Хосту №0 (нашим воротам)
            // И пробрасывает это событие в Lua!
            DynamicComponent.Lua.LuaManager.DeliverEventToLua(0, 0);
        }
        
        private static EventSubscription _eventSubscriptionPool;
        private static TypeChain _globalTypeChain;
        // Public setter to bind the active DCS event infrastructure to the Lua bridge
        public static void BindEventSystems(EventSubscription subPool, TypeChain typeChain)
        {
            _eventSubscriptionPool = subPool;
            _globalTypeChain = typeChain;
        }

        private void InitializeGlobalEngine()
        {
            try
            {
                Debug.Log("[LuaManager] Starting global virtual machine instance...");
                _globalLuaState = new LuaStateWrapper("GlobalEngine");

                IntPtr L = _globalLuaState.L;

                // Existing registry reflection data functions bindings
                IntPtr countCallbackPtr = Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_GetTypesCount);
                LuaNative.lua_pushcclosure(L, countCallbackPtr, 0);
                LuaNative.lua_setglobal(L, "DCS_Internal_GetTypesCount");

                IntPtr nameCallbackPtr = Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_GetTypeNameById);
                LuaNative.lua_pushcclosure(L, nameCallbackPtr, 0);
                LuaNative.lua_setglobal(L, "DCS_Internal_GetTypeNameById");

                IntPtr createHostPtr = Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_CreateHost);
                LuaNative.lua_pushcclosure(L, createHostPtr, 0);
                LuaNative.lua_setglobal(L, "DCS_Internal_CreateHost");

                IntPtr addCompPtr = Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_AddComponent);
                LuaNative.lua_pushcclosure(L, addCompPtr, 0);
                LuaNative.lua_setglobal(L, "DCS_Internal_AddComponent");

                // 2. ADD THIS EXACT LINE: Binding the new permanent subscription bridge to Lua scope
                IntPtr subPtr = Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_RegisterSubscription);
                LuaNative.lua_pushcclosure(L, subPtr, 0);
                LuaNative.lua_setglobal(L, "DCS_Internal_RegisterSubscription");

                string bootstrapPath = $"{LuaRootPath}/Core/bootstrap.lua";

                if (File.Exists(bootstrapPath))
                {
                    string bootstrapCode = File.ReadAllText(bootstrapPath);
                    _globalLuaState.ExecuteString(bootstrapCode, "bootstrap.lua");
                }
                else
                {
                    Debug.LogError($"[LuaManager] Critical Error: Entry point bootstrap script not found at path: {bootstrapPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LuaManager] Failed to spin up the script engine framework: {e.Message}");
            }
        }

        // ============================================================
        //  ADD THIS EXACT METHOD INSIDE LUA_MANAGER CLASS
        // ============================================================
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_RegisterSubscription(IntPtr L)
        {
            // Read target unboxed integers from the native execution stack
            int hostId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int eventTypeId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            ref HostData hostData = ref HostManager.GlobalHosts[hostId];
            Host host = new Host { Id = hostId, Generation = hostData.Generation };

            // Secure subscription allocation straight via your high-performance DCS architecture
            if (_eventSubscriptionPool != null && _globalHostChain != null && _globalTypeChain != null)
            {
                // FIX: Directly invoking the concrete class instance instead of casting to IComponentPool interface
                _eventSubscriptionPool.SystemSubscribe(host, eventTypeId, _globalHostChain, _globalTypeChain);

                Debug.Log($"<color=cyan>[DCS LUA SUBSCRIPTION]</color> Host {hostId} permanently registered subscription to Event TypeId: {eventTypeId}");
            }

            return 0;
        }

        // ============================================================
        //  GLOBAL METADATA REFLECTION CALLBACKS
        // ============================================================

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
            string typeName = ComponentRegistry.GetTypeNameById(id);
            LuaNative.lua_pushstring(L, typeName);
            return 1;
        }

        // ============================================================
        //  GLOBAL HIGH-PERFORMANCE DCS ALLOCATION BRIDGES
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_CreateHost(IntPtr L)
        {
            Host newHost = HostManager.CreateHost();
            LuaNative.lua_pushinteger(L, newHost.Id);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_AddComponent(IntPtr L)
        {
            int hostId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int typeId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            ref HostData hostData = ref HostManager.GlobalHosts[hostId];
            Host host = new Host { Id = hostId, Generation = hostData.Generation };

            if (typeId >= 0 && typeId < ComponentRegistry.MaxComponentTypes)
            {
                IComponentPool targetPool = ComponentRegistry.Pools[typeId];

                // FIX: Validating static chain context reference and routing allocation via interface method
                if (targetPool != null && _globalHostChain != null)
                {
                    targetPool.SystemAllocate(host, _globalHostChain);
                    return 0;
                }
            }

            Debug.LogWarning($"[Lua DCS Bridge] Error: Allocation crash. TypeId: {typeId} on Host ID: {hostId}");
            return 0;
        }

        // ============================================================
        //  THE EVENT SYSTEM
        // ============================================================

        /// <summary>
        /// Delivers a runtime DCS event straight into the global Lua execution context.
        /// </summary>
        /// <param name="hostId">The ID of the Host that generated or received the event.</param>
        /// <param name="eventTypeId">The ComponentType ID of the event.</param>
        public static void DeliverEventToLua(int hostId, int eventTypeId)
        {
            if (_instance == null || _instance._globalLuaState == null) return;

            IntPtr L = _instance._globalLuaState.L;

            // 1. Find the global event router function we will define in bootstrap.lua
            LuaNative.lua_getglobal(L, "DCS_Global_EventRouter");

            // If the function exists on the stack, push the arguments and call it
            if (LuaNative.lua_isfunction(L, -1))
            {
                LuaNative.lua_pushinteger(L, hostId);
                LuaNative.lua_pushinteger(L, eventTypeId);

                // Call the function with 2 arguments and 0 results
                if (LuaNative.lua_pcallk(L, 2, 0, 0, 0, IntPtr.Zero) != 0)
                {
                    string error = _instance._globalLuaState.GetStringFromStack(-1);
                    Debug.LogError($"[Lua Event Router Error] {error}");
                }
            }
        }
    }
}
