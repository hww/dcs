using DCS.Core;
using System;
using System.Runtime.InteropServices;

namespace DCS.Lua.Bindings
{
    public static class ComponentBindings
    {
        public static void Register(IntPtr L, int tableIndex)
        {
            LuaBindings.RegisterMethod(L, Lua_CreateComponent, tableIndex, "CreateComponent");
            LuaBindings.RegisterMethod(L, Lua_RemoveComponent, tableIndex, "RemoveComponent");
            LuaBindings.RegisterMethod(L, Lua_HasComponent, tableIndex, "HasComponent");
            LuaBindings.RegisterMethod(L, Lua_GetComponent, tableIndex, "GetComponent");
            LuaBindings.RegisterMethod(L, Lua_GetField, tableIndex, "GetField");
            LuaBindings.RegisterMethod(L, Lua_TryGetField, tableIndex, "TryGetField");
            LuaBindings.RegisterMethod(L, Lua_SetField, tableIndex, "SetField");
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_CreateComponent(IntPtr L)
        {
            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            int typeId = LuaArgumentReader.ReadInt(L, 2);
            int packedHost = LuaArgumentReader.ReadInt(L, 3);

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            Host host = Host.FromLua(packedHost);
            if (!HostManager.IsValid(host))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            var pool = ComponentRegistry.Pools[typeId];
            if (pool == null)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            Handle handle = pool.SystemAllocate(host, domain.HostChain);
            if (handle.IsNull)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            LuaNative.lua_pushinteger(L, handle.Pack());
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_RemoveComponent(IntPtr L)
        {
            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
                return 0;

            int typeId = LuaArgumentReader.ReadInt(L, 2);
            int packedHandle = LuaArgumentReader.ReadInt(L, 3);

            if (packedHandle == HandleConfig.NULL_INDEX ||
                typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
                return 0;

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];
            if (pool == null)
                return 0;

            if (pool.TryGetHost(handle, out Host host))
                pool.SystemFree(host, domain.HostChain, handle);

            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_HasComponent(IntPtr L)
        {
            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            int typeId = LuaArgumentReader.ReadInt(L, 2);
            int packedHost = LuaArgumentReader.ReadInt(L, 3);

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            Host host = Host.FromLua(packedHost);
            if (!HostManager.IsValid(host))
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            ChainNode node = domain.HostChain.GetTypedHandle(host, typeId);
            LuaNative.lua_pushboolean(L, node.IsNull ? 0 : 1);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetComponent(IntPtr L)
        {
            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            int typeId = LuaArgumentReader.ReadInt(L, 2);
            int packedHost = LuaArgumentReader.ReadInt(L, 3);

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            Host host = Host.FromLua(packedHost);
            if (!HostManager.IsValid(host))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            ChainNode node = domain.HostChain.GetTypedHandle(host, typeId);
            if (node.IsNull)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            LuaNative.lua_pushinteger(L, node.Component.Pack());
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetField(IntPtr L)
        {
            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            int typeId = LuaArgumentReader.ReadInt(L, 2);
            int packedHandle = LuaArgumentReader.ReadInt(L, 3);
            string fieldName = LuaArgumentReader.ReadString(L, 4);

            if (packedHandle == HandleConfig.NULL_INDEX ||
                string.IsNullOrEmpty(fieldName) ||
                typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];
            if (pool == null || !pool.TryGetDenseIndex(handle, out int denseIndex))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            int topBefore = LuaNative.lua_gettop(L);
            if (!pool.GetField(denseIndex, fieldName, L))
                return LuaNative.luaL_error(L, $"[DCS Error] Field '{fieldName}' does not exist.");

            int count = LuaNative.lua_gettop(L) - topBefore;
            return count > 0 ? count : 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_TryGetField(IntPtr L)
        {
            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
            {
                LuaNative.lua_pushboolean(L, 0);
                LuaNative.lua_pushnil(L);
                return 2;
            }

            int typeId = LuaArgumentReader.ReadInt(L, 2);
            int packedHandle = LuaArgumentReader.ReadInt(L, 3);
            string fieldName = LuaArgumentReader.ReadString(L, 4);

            if (packedHandle == HandleConfig.NULL_INDEX ||
                string.IsNullOrEmpty(fieldName) ||
                typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
            {
                LuaNative.lua_pushboolean(L, 0);
                LuaNative.lua_pushnil(L);
                return 2;
            }

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];
            if (pool == null || !pool.TryGetDenseIndex(handle, out int denseIndex))
            {
                LuaNative.lua_pushboolean(L, 0);
                LuaNative.lua_pushnil(L);
                return 2;
            }

            int topBefore = LuaNative.lua_gettop(L);
            if (!pool.GetField(denseIndex, fieldName, L))
            {
                LuaNative.lua_pushboolean(L, 0);
                LuaNative.lua_pushnil(L);
                return 2;
            }

            int valueCount = LuaNative.lua_gettop(L) - topBefore;
            LuaNative.lua_pushboolean(L, 1);
            LuaNative.lua_rotate(L, -(valueCount + 1), 1);
            return valueCount + 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_SetField(IntPtr L)
        {
            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
                return 0;

            int typeId = LuaArgumentReader.ReadInt(L, 2);
            int packedHandle = LuaArgumentReader.ReadInt(L, 3);
            string fieldName = LuaArgumentReader.ReadString(L, 4);

            if (packedHandle == HandleConfig.NULL_INDEX ||
                string.IsNullOrEmpty(fieldName) ||
                typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
                return 0;

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];
            if (pool == null || !pool.TryGetDenseIndex(handle, out int denseIndex))
                return 0;

            if (!pool.SetField(denseIndex, fieldName, L))
            {
                return LuaNative.luaL_error(L,
                    $"[DCS Write Error] Cannot write field '{fieldName}' on type {typeId}.");
            }

            return 0;
        }
    }
}