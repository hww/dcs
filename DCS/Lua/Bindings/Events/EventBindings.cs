using System;
using System.Runtime.InteropServices;
using DCS.Core;

namespace DCS.Lua.Bindings
{
    /// <summary>
    /// Low-level bridge between Lua and the existing DCS EventSystem.
    /// Existing global function names are preserved.
    /// </summary>
    public static class EventBindings
    {
        public static void Register(IntPtr L)
        {
            RegisterGlobalFunction(L, Lua_EmitEvent, "DCS_EmitEvent");
            RegisterGlobalFunction(L, Lua_RegisterSubscription, "DCS_RegisterSubscription");
            RegisterGlobalFunction(L, Lua_DeliverEvent, "DCS_DeliverEvent");
        }

        private static void RegisterGlobalFunction(IntPtr L, Func<IntPtr, int> fn, string name)
        {
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(fn);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_setglobal(L, name);
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_EmitEvent(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int hostId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            if (!TryGetHost(hostId, out Host host))
                return 0;

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
                return 0;

            var pool = ComponentRegistry.Pools[typeId];
            if (pool == null || LuaManager._globalHostChain == null)
                return 0;

            Handle handle = pool.SystemAllocate(host, LuaManager._globalHostChain);
            if (!handle.IsNull)
                LuaManager.DeliverEventToLua(hostId, typeId, handle.Pack());

            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_RegisterSubscription(IntPtr L)
        {
            int hostId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int eventTypeId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            if (!TryGetHost(hostId, out Host host))
                return 0;

            if (eventTypeId < 0 || eventTypeId >= ComponentRegistry.MaxComponentTypes)
                return 0;

            if (LuaManager._eventSubscriptionPool != null &&
                LuaManager._globalHostChain != null &&
                LuaManager._globalTypeChain != null)
            {
                LuaManager._eventSubscriptionPool.SystemSubscribe(
                    host,
                    eventTypeId,
                    LuaManager._globalHostChain,
                    LuaManager._globalTypeChain);
            }

            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_DeliverEvent(IntPtr L)
        {
            int receiverHostId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int eventTypeId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 3, IntPtr.Zero);

            if (receiverHostId < 0 || packedHandle == HandleConfig.NULL_INDEX)
                return 0;

            if (eventTypeId < 0 || eventTypeId >= ComponentRegistry.MaxComponentTypes)
                return 0;

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[eventTypeId];

            if (pool != null && pool.TryGetDenseIndex(handle, out _))
                LuaManager.DeliverEventToLua(receiverHostId, eventTypeId, packedHandle);

            return 0;
        }

        private static bool TryGetHost(int hostId, out Host host)
        {
            host = default;

            if (hostId < 0 || hostId >= HostManager.GlobalHosts.Length)
                return false;

            ref HostData data = ref HostManager.GlobalHosts[hostId];
            host = new Host
            {
                Id = (ushort)hostId,
                Generation = data.Generation
            };

            return HostManager.IsValid(host);
        }
    }
}
