using System;
using System.Runtime.InteropServices;
using DCS.Core;
using DCS.Gameplay;

namespace DCS.Lua.Bindings
{
    public static class AIBindings
    {
        public static void Register(IntPtr L)
        {
            LuaNative.lua_newtable(L);
            LuaBindings.RegisterMethod(L, Lua_SetCombatRole, "SetCombatRole");
            LuaBindings.RegisterMethod(L, Lua_GetCombatRole, "GetCombatRole");
            LuaNative.lua_setglobal(L, "AI");
        }

        // ------------------------------------------------------------
        //  AI.SetCombatRole(chainId, hostId, role, strongPointId) -> handle
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_SetCombatRole(IntPtr L)
        {
            int chainId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int hostId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            int role = (int)LuaNative.lua_tointegerx(L, 3, IntPtr.Zero);
            int strongPointId = (int)LuaNative.lua_tointegerx(L, 4, IntPtr.Zero);

            HostChain chain = DomainRegistry.Get(chainId).HostChain;
            if (chain == null)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            if (!TryGetHost(hostId, out Host host))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            // Reuse existing component if present, else allocate.
            Handle handle;
            var pool = ComponentRegistry.GetPool<CombatRoleComponent>();
            ChainNode existing = chain.GetTypedHandle(
                host, ComponentType<CombatRoleComponent>.Id);

            if (!existing.IsNull)
            {
                handle = existing.Component;
            }
            else
            {
                handle = pool.Allocate(host, chain);
            }

            if (handle.IsNull)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            ref CombatRoleComponent comp = ref pool.ResolveHandle(handle);
            comp.Role = (CombatRole)role;
            comp.StrongPointId = strongPointId;
            comp.OwnerHostId = hostId;

            LuaNative.lua_pushinteger(L, handle.Pack());
            return 1;
        }

        // ------------------------------------------------------------
        //  AI.GetCombatRole(chainId, hostId) -> role, strongPointId  (or nil)
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetCombatRole(IntPtr L)
        {
            int chainId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int hostId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            HostChain chain = DomainRegistry.Get(chainId).HostChain;
            if (chain == null || !TryGetHost(hostId, out Host host))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            ChainNode node = chain.GetTypedHandle(
                host, ComponentType<CombatRoleComponent>.Id);

            if (node.IsNull)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            var pool = ComponentRegistry.GetPool<CombatRoleComponent>();
            ref CombatRoleComponent comp = ref pool.ResolveHandle(node.Component);

            LuaNative.lua_pushinteger(L, (int)comp.Role);
            LuaNative.lua_pushinteger(L, comp.StrongPointId);
            return 2;
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