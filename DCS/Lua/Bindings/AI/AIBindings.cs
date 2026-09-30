using DCS.Actors;
using DCS.Core;
using System;
using System.Runtime.InteropServices;
using AOT;

namespace DCS.Lua
{
    public static class AIBindings
    {
        public static void Register(IntPtr L)
        {
            LuaBindings.RegisterNamespace(L, "AI", (state, tableIndex) =>
            {
                LuaBindings.RegisterMethod(state, Lua_SetCombatRole, tableIndex, "SetCombatRole");
                LuaBindings.RegisterMethod(state, Lua_GetCombatRole, tableIndex, "GetCombatRole");
            });
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_SetCombatRole(IntPtr L)
        {
            var args = new ArgReader(L, "AI.SetCombatRole");
            args.ExpectExactly(4);

            args.CheckInteger(1);
            int hostId = (int)args.CheckInteger(2);
            int role = (int)args.CheckInteger(3);
            int strongPointId = (int)args.CheckInteger(4);

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            if (!HostResolver.TryGetHost(hostId, out Host host))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            var pool = ComponentRegistry.GetPool<CombatRoleComponent>();
            ChainNode existing = domain.HostChain.GetTypedHandle(
                host, ComponentType<CombatRoleComponent>.Id);

            Handle handle;
            if (!existing.IsNull)
            {
                handle = existing.Component;
            }
            else
            {
                handle = pool.Allocate(host, domain.HostChain);
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

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetCombatRole(IntPtr L)
        {
            var args = new ArgReader(L, "AI.GetCombatRole");
            args.ExpectExactly(2);

            args.CheckInteger(1);
            int hostId = (int)args.CheckInteger(2);

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            if (!HostResolver.TryGetHost(hostId, out Host host))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            ChainNode node = domain.HostChain.GetTypedHandle(
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
    }
}