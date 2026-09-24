using System;
using System.Runtime.InteropServices;
using DCS.Core;

namespace DCS.Lua.Bindings
{
    public static class EventBindings
    {
        public static void Register(IntPtr L)
        {
            int topBefore = LuaNative.lua_gettop(L);

            LuaNative.lua_getglobal(L, "DCS");
            if (LuaNative.lua_type(L, -1) != LuaNative.LUA_TTABLE)
            {
                LuaNative.lua_settop(L, topBefore);
                return;
            }

            LuaBindings.RegisterMethod(L, Lua_EmitEvent, "EmitEvent");
            LuaBindings.RegisterMethod(L, Lua_RegisterSubscription, "RegisterSubscription");
            LuaBindings.RegisterMethod(L, Lua_UnregisterSubscription, "UnregisterSubscription");
            LuaBindings.RegisterMethod(L, Lua_DeliverEvent, "DeliverEvent");

            LuaNative.lua_settop(L, topBefore);
        }

        // ------------------------------------------------------------
        //  DCS.EmitEvent(domainId, eventTypeId, hostId) -> void
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_EmitEvent(IntPtr L)
        {
            int domainId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int typeId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            int hostId = (int)LuaNative.lua_tointegerx(L, 3, IntPtr.Zero);

            var domain = DomainRegistry.Get(domainId);
            if (domain == null)
                return LuaNative.lua_error(L,
                    $"[EventBindings] EmitEvent: domain id {domainId} not found");

            HostChain chain = domain.HostChain;
            if (chain == null)
                return LuaNative.lua_error(L,
                    $"[EventBindings] EmitEvent: domain {domainId} has no HostChain");

            if (!TryGetHost(hostId, out Host host))
                return LuaNative.lua_error(L,
                    $"[EventBindings] EmitEvent: invalid host {hostId}");

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
                return LuaNative.lua_error(L,
                    $"[EventBindings] EmitEvent: type id {typeId} out of range [0, {ComponentRegistry.MaxComponentTypes})");

            var pool = ComponentRegistry.Pools[typeId];
            if (pool == null)
                return LuaNative.lua_error(L,
                    $"[EventBindings] EmitEvent: no pool for type id {typeId}");

            Handle handle = pool.SystemAllocate(host, chain);
            if (handle.IsNull)
                return LuaNative.lua_error(L,
                    $"[EventBindings] EmitEvent: allocation failed for type {typeId} on host {hostId}");

            LuaManager.DeliverEventToLua(hostId, typeId, handle.Pack());
            return 0;
        }

        // ------------------------------------------------------------
        //  DCS.RegisterSubscription(domainId, eventTypeId, hostId) -> void
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_RegisterSubscription(IntPtr L)
        {
            int domainId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int hostId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            int eventTypeId = (int)LuaNative.lua_tointegerx(L, 3, IntPtr.Zero);

            Domain domain = DomainRegistry.Get(domainId);
            if (domain == null)
                return LuaNative.lua_error(L,
                    $"[EventBindings] RegisterSubscription: domain id {domainId} not found");

            if (domain.SubscriptionPool == null)
                return LuaNative.lua_error(L,
                    $"[EventBindings] RegisterSubscription: domain {domainId} has no SubscriptionPool");

            if (!TryGetHost(hostId, out Host host))
            {
                // Не ошибка — просто нет валидного хоста. Возвращаем nil.
                LuaNative.lua_pushnil(L);
                return 1;
            }

            if (eventTypeId < 0 || eventTypeId >= ComponentRegistry.MaxComponentTypes)
                return LuaNative.lua_error(L,
                    $"[EventBindings] RegisterSubscription: event type id {eventTypeId} out of range");

            Handle subHandle = domain.SubscriptionPool.SystemSubscribe(
                host,
                eventTypeId,
                domain.HostChain,
                domain.TypeChain);

            if (subHandle.IsNull)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            LuaNative.lua_pushinteger(L, subHandle.Pack());
            return 1;
        }

        // ------------------------------------------------------------
        //  DCS.UnregisterSubscription(domainId, packedSubHandle) -> void
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_UnregisterSubscription(IntPtr L)
        {
            int domainId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int packedSubHandle = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            if (packedSubHandle == HandleConfig.NULL_INDEX)
                return 0;

            Domain domain = DomainRegistry.Get(domainId);
            if (domain == null)
                return LuaNative.lua_error(L,
                    $"[EventBindings] UnregisterSubscription: domain id {domainId} not found");

            var pool = domain.SubscriptionPool;
            if (pool == null)
                return LuaNative.lua_error(L,
                    $"[EventBindings] UnregisterSubscription: domain {domainId} has no SubscriptionPool");

            Handle subHandle = new Handle(packedSubHandle);

            if (!pool.TryGetHost(subHandle, out Host host))
                return 0; // уже протух — не ошибка

            domain.SubscriptionPool.FreeSubscription(
                host,
                domain.HostChain,
                domain.TypeChain,
                ref subHandle);

            return 0;
        }

        // ------------------------------------------------------------
        //  DCS.DeliverEvent(receiverHostId, eventTypeId, packedHandle)
        //  domainId не нужен — доставка идёт по конкретному хосту/пулу
        //
        //  Внимание: эта функция может вызываться из C#-горячего пути
        //  (через SubscriptionNode.ReceiveMessage). lua_error здесь
        //  размотает стек через pcall и прервёт текущую итерацию
        //  EventSystem.DeliverEvents. Поэтому lua_error — только для
        //  явно битых аргументов, а «handle уже мёртв» — тихий no-op.
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_DeliverEvent(IntPtr L)
        {
            int receiverHostId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int eventTypeId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 3, IntPtr.Zero);

            if (packedHandle == HandleConfig.NULL_INDEX)
                return 0;

            if (receiverHostId < 0 || receiverHostId >= HostManager.GlobalHosts.Length)
                return LuaNative.lua_error(L,
                    $"[EventBindings] DeliverEvent: receiverHostId {receiverHostId} out of range [0, {HostManager.GlobalHosts.Length})");

            if (eventTypeId < 0 || eventTypeId >= ComponentRegistry.MaxComponentTypes)
                return LuaNative.lua_error(L,
                    $"[EventBindings] DeliverEvent: event type id {eventTypeId} out of range [0, {ComponentRegistry.MaxComponentTypes})");

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[eventTypeId];

            if (pool == null)
                return LuaNative.lua_error(L,
                    $"[EventBindings] DeliverEvent: no pool for event type id {eventTypeId}");

            // Handle уже мёртв — это не ошибка, а гонка с ClearFramePool.
            if (!pool.TryGetDenseIndex(handle, out _))
                return 0;

            LuaManager.DeliverEventToLua(receiverHostId, eventTypeId, packedHandle);
            return 0;
        }

        private static bool TryGetHost(int hostId, out Host host)
        {
            host = default;
            if (hostId < 0 || hostId >= HostManager.GlobalHosts.Length)
                return false;
            ref HostData data = ref HostManager.GlobalHosts[hostId];
            host = new Host { Id = (ushort)hostId, Generation = data.Generation };
            return HostManager.IsValid(host);
        }
    }
}