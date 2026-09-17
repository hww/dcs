using System;
using System.Runtime.InteropServices;
using DCS.Core;
using DCS.Gameplay;

namespace DCS.Lua.Bindings
{
    /// <summary>
    /// Lua-facing AI API. Exposes the global table "AI" with methods
    /// to control agent combat roles from scripts.
    ///
    /// Usage from Lua:
    ///   AI.SetCombatRole(hostId, CombatRole.Ambusher, strongPointId)
    /// </summary>
    public static class AIBindings
    {
        public static void Register(IntPtr L)
        {
            LuaNative.lua_newtable(L);
            RegisterMethod(L, "SetCombatRole", Lua_SetCombatRole);
            RegisterMethod(L, "GetCombatRole", Lua_GetCombatRole);
            LuaNative.lua_setglobal(L, "AI");
        }

        private static void RegisterMethod(IntPtr L, string name, Func<IntPtr, int> fn)
        {
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(fn);
            LuaNative.lua_pushstring(L, name);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_settable(L, -3);
        }

        // ------------------------------------------------------------
        //  AI.SetCombatRole(hostId, role, strongPointId) -> handle
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_SetCombatRole(IntPtr L)
        {
            int hostId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int role = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            int strongPointId = (int)LuaNative.lua_tointegerx(L, 3, IntPtr.Zero);

            if (!TryGetHost(hostId, out Host host))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            if (LuaManager._globalHostChain == null)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            // Reuse existing component if present, else allocate.
            Handle handle;
            var pool = ComponentRegistry.GetPool<CombatRoleComponent>();
            ChainNode existing = LuaManager._globalHostChain.GetTypedHandle(
                host, ComponentType<CombatRoleComponent>.Id);

            if (!existing.IsNull)
            {
                handle = existing.Component;
            }
            else
            {
                handle = pool.Allocate(host, LuaManager._globalHostChain);
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
        //  AI.GetCombatRole(hostId) -> role, strongPointId  (or nil)
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetCombatRole(IntPtr L)
        {
            int hostId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);

            if (!TryGetHost(hostId, out Host host) || LuaManager._globalHostChain == null)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            ChainNode node = LuaManager._globalHostChain.GetTypedHandle(
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