using DCS.Core;
using System;
using System.Runtime.InteropServices;

namespace DCS.Lua
{
    public static class EventBindings
    {
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
            var args = new ArgReader(L, "EmitEvent");
            args.ExpectExactly(3);

            args.CheckInteger(1);
            int typeId = (int)args.CheckInteger(2);
            int hostId = (int)args.CheckInteger(3);

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
                return 0;

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
                return 0;

            if (!HostResolver.TryGetHost(hostId, out Host host))
                return 0;

            var pool = ComponentRegistry.Pools[typeId];
            if (pool == null)
                return 0;

            Handle handle = pool.SystemAllocate(host, domain.HostChain);
            if (!handle.IsNull)
                LuaManager.Current?.CallEventRouter(hostId, typeId, handle.Pack());

            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_RegisterSubscription(IntPtr L)
        {
            var args = new ArgReader(L, "RegisterSubscription");
            args.ExpectExactly(3);

            args.CheckInteger(1);
            int hostId = (int)args.CheckInteger(2);
            int eventTypeId = (int)args.CheckInteger(3);

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
                return 0;

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
            var args = new ArgReader(L, "UnregisterSubscription");
            args.ExpectExactly(3);

            args.CheckInteger(1);
            int hostId = (int)args.CheckInteger(2);
            int packedSubHandle = (int)args.CheckInteger(3);

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
                return 0;

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
            var args = new ArgReader(L, "DeliverEvent");
            args.ExpectExactly(4);

            args.CheckInteger(1);
            int receiverHostId = (int)args.CheckInteger(2);
            int eventTypeId = (int)args.CheckInteger(3);
            int packedHandle = (int)args.CheckInteger(4);

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
                return 0;

            if (receiverHostId < 0 || packedHandle == HandleConfig.NULL_INDEX)
                return 0;

            if (eventTypeId < 0 || eventTypeId >= ComponentRegistry.MaxComponentTypes)
                return 0;

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[eventTypeId];
            if (pool != null && pool.TryGetDenseIndex(handle, out _))
                LuaManager.Current?.CallEventRouter(receiverHostId, eventTypeId, packedHandle);

            return 0;
        }
    }
}