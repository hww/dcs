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

            // 1. Initialize your high-performance DCS pools
            ComponentRegistry.InitializeAllPools();

            // 2. Create the main component chain manager for the game
            HostChain myGameChain = new HostChain();

            // 3. Link this active C# chain to the global Lua infrastructure
            DynamicComponent.Lua.LuaManager.BindHostChain(myGameChain);
        }

        private void Start()
        {
            // Ensure pools are fully initialized in your architecture before spinning up the state
            InitializeGlobalEngine();
        }

        private void InitializeGlobalEngine()
        {
            try
            {
                Debug.Log("[LuaManager] Starting global virtual machine instance...");
                _globalLuaState = new LuaStateWrapper("GlobalEngine");

                IntPtr L = _globalLuaState.L;

                // 1. Bind registry reflection data functions
                IntPtr countCallbackPtr = Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_GetTypesCount);
                LuaNative.lua_pushcclosure(L, countCallbackPtr, 0);
                LuaNative.lua_setglobal(L, "DCS_Internal_GetTypesCount");

                IntPtr nameCallbackPtr = Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_GetTypeNameById);
                LuaNative.lua_pushcclosure(L, nameCallbackPtr, 0);
                LuaNative.lua_setglobal(L, "DCS_Internal_GetTypeNameById");

                // 2. GLOBAL LEVEL REGISTRATION: Core DCS allocation pipelines live here now
                IntPtr createHostPtr = Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_CreateHost);
                LuaNative.lua_pushcclosure(L, createHostPtr, 0);
                LuaNative.lua_setglobal(L, "DCS_Internal_CreateHost");

                IntPtr addCompPtr = Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_AddComponent);
                LuaNative.lua_pushcclosure(L, addCompPtr, 0);
                LuaNative.lua_setglobal(L, "DCS_Internal_AddComponent");

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
    }
}
