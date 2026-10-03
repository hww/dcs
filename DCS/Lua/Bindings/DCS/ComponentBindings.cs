using DCS.Core;
using System;
using System.Runtime.InteropServices;

namespace DCS.Lua
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

        // ============================================================
        //  CreateComponent(domain, typeId, hostId) -> handle | nil
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_CreateComponent(IntPtr L)
        {
            var args = new ArgReader(L, "CreateComponent");
            args.ExpectExactly(3);

            // Аргумент 1 — domain (userdata). Проверку делает HostResolver.
            args.CheckInteger(1);
            int typeId = (int)args.CheckInteger(2);
            int packedHost = (int)args.CheckInteger(3);

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

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

        // ============================================================
        //  RemoveComponent(domain, typeId, handle)
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_RemoveComponent(IntPtr L)
        {
            var args = new ArgReader(L, "RemoveComponent");
            args.ExpectExactly(3);

            args.CheckInteger(1);
            int typeId = (int)args.CheckInteger(2);
            int packedHost = (int)args.CheckInteger(3);
            int packedHandle = (int)args.CheckInteger(4);

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
                return 0;

            if (packedHandle == HandleConfig.NULL_INDEX ||
                typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
                return 0;

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];
            if (pool == null)
                return 0;

            pool.SystemFree(new Host(packedHost), domain.HostChain, handle);

            return 0;
        }

        // ============================================================
        //  HasComponent(domain, typeId, hostId) -> bool
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_HasComponent(IntPtr L)
        {
            var args = new ArgReader(L, "HasComponent");
            args.ExpectExactly(3);

            args.CheckInteger(1);
            int typeId = (int)args.CheckInteger(2);
            int packedHost = (int)args.CheckInteger(3);

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

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

        // ============================================================
        //  GetComponent(domain, typeId, hostId) -> handle | nil
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetComponent(IntPtr L)
        {
            var args = new ArgReader(L, "GetComponent");
            args.ExpectExactly(3);

            args.CheckInteger(1);
            int typeId = (int)args.CheckInteger(2);
            int packedHost = (int)args.CheckInteger(3);

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

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

        // ============================================================
        //  GetField(domain, typeId, handle, fieldName) -> value
        //  Если поле не найдено — luaL_error.
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetField(IntPtr L)
        {
            var args = new ArgReader(L, "GetField");
            args.ExpectExactly(4);

            args.CheckInteger(1);
            int typeId = (int)args.CheckInteger(2);
            int packedHandle = (int)args.CheckInteger(3);
            string fieldName = args.CheckString(4);

            if (string.IsNullOrEmpty(fieldName))
                LuaFail.Fail(L, "GetField", "argument #4: field name must not be empty");

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            if (packedHandle == HandleConfig.NULL_INDEX ||
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
                LuaFail.Fail(L, "GetField", $"field '{fieldName}' does not exist on type {typeId}");

            int count = LuaNative.lua_gettop(L) - topBefore;
            return count > 0 ? count : 1;
        }

        // ============================================================
        //  TryGetField(domain, typeId, handle, fieldName) -> bool, value
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_TryGetField(IntPtr L)
        {
            var args = new ArgReader(L, "TryGetField");
            args.ExpectExactly(5);

            args.CheckInteger(1);
            int typeId = (int)args.CheckInteger(2);
            int packedHandle = (int)args.CheckInteger(3);
            string fieldName = args.CheckString(4);

            if (string.IsNullOrEmpty(fieldName))
                LuaFail.Fail(L, "TryGetField", "argument #4: field name must not be empty");

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
            {
                LuaNative.lua_pushboolean(L, 0);
                LuaNative.lua_pushnil(L);
                return 2;
            }

            if (packedHandle == HandleConfig.NULL_INDEX ||
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

        // ============================================================
        //  SetField(domain, typeId, handle, fieldName, value)
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_SetField(IntPtr L)
        {
            var args = new ArgReader(L, "SetField");
            args.ExpectExactly(5);

            args.CheckInteger(1);
            int typeId = (int)args.CheckInteger(2);
            int packedHandle = (int)args.CheckInteger(3);
            string fieldName = args.CheckString(4);
            args.CheckAny(5);   // значение — любой тип, включая nil

            if (string.IsNullOrEmpty(fieldName))
                LuaFail.Fail(L, "SetField", "argument #4: field name must not be empty");

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
                return 0;

            if (packedHandle == HandleConfig.NULL_INDEX ||
                typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
                return 0;

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];
            if (pool == null || !pool.TryGetDenseIndex(handle, out int denseIndex))
                return 0;

            if (!pool.SetField(denseIndex, fieldName, L))
                LuaFail.Fail(L, "SetField",
                    $"cannot write field '{fieldName}' on type {typeId}");

            return 0;
        }
    }
}