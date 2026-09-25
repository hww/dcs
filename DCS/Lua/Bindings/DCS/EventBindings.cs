using DCS.Core;
using System;
using System.Runtime.InteropServices;

namespace DCS.Lua.Bindings
{
    public static class EventBindings
    {
        // EventBindings.cs
        public static void Register(IntPtr L, int tableIndex)
        {
            LuaBindings.RegisterMethod(L, Lua_EmitEvent, tableIndex, "EmitEvent");
            LuaBindings.RegisterMethod(L, Lua_RegisterSubscription, tableIndex, "RegisterSubscription");
            LuaBindings.RegisterMethod(L, Lua_UnregisterSubscription, tableIndex, "UnregisterSubscription");
            LuaBindings.RegisterMethod(L, Lua_DeliverEvent, tableIndex, "DeliverEvent");
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_EmitEvent(IntPtr L)
        {
            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
                return 0;

            int typeId = LuaArgumentReader.ReadInt(L, 2);
            int hostId = LuaArgumentReader.ReadInt(L, 3);

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
                return 0;

            if (!HostResolver.TryGetHost(hostId, out Host host))
                return 0;

            var pool = ComponentRegistry.Pools[typeId];
            if (pool == null)
                return 0;

            Handle handle = pool.SystemAllocate(host, domain.HostChain);
            if (!handle.IsNull)
                LuaManager.DeliverEventToLua(hostId, typeId, handle.Pack());

            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_RegisterSubscription(IntPtr L)
        {
            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
                return 0;

            int hostId = LuaArgumentReader.ReadInt(L, 2);
            int eventTypeId = LuaArgumentReader.ReadInt(L, 3);

            if (!HostResolver.TryGetHost(hostId, out Host host))
                return 0;

            if (eventTypeId < 0 || eventTypeId >= ComponentRegistry.MaxComponentTypes)
                return 0;

            domain.SubscriptionPool.SystemSubscribe(
                host,
                eventTypeId,
                domain.HostChain,
                domain.TypeChain);

            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_UnregisterSubscription(IntPtr L)
        {
            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
                return 0;

            int hostId = LuaArgumentReader.ReadInt(L, 2);
            int packedSubHandle = LuaArgumentReader.ReadInt(L, 3);

            if (!HostResolver.TryGetHost(hostId, out Host host))
                return 0;

            if (packedSubHandle == HandleConfig.NULL_INDEX)
                return 0;

            Handle subHandleToFree = new Handle(packedSubHandle);
            domain.SubscriptionPool.FreeSubscription(
                host,
                domain.HostChain,
                domain.TypeChain,
                ref subHandleToFree);

            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_DeliverEvent(IntPtr L)
        {
            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
                return 0;

            int receiverHostId = LuaArgumentReader.ReadInt(L, 2);
            int eventTypeId = LuaArgumentReader.ReadInt(L, 3);
            int packedHandle = LuaArgumentReader.ReadInt(L, 4);

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
    }
}