using DCS.Core;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DCS.Lua.Bindings
{
    /// <summary>
    /// Low-level DCS/ECS Lua API.
    /// Existing global names are intentionally preserved.
    ///
    /// Convention: Lua always passes Host values as Pack() (Id | Generation &lt;&lt; 16).
    /// Handles are also passed as Pack(). C# unpacks before touching GlobalHosts.
    /// </summary>
    public static class DcsBindings
    {
        public static void Register(IntPtr L)
        {
            // Внутренние — глобально (нужны до создания таблицы)
            LuaBindings.RegisterGlobalFunction(L, Lua_GetTypesCount, "Internal_GetTypesCount");
            LuaBindings.RegisterGlobalFunction(L, Lua_GetTypeNameById, "Internal_GetTypeNameById");

            // Остальное — в таблице DCS
            LuaNative.lua_newtable(L);
            LuaBindings.RegisterMethod(L, Lua_CreateComponent, "CreateComponent");
            LuaBindings.RegisterMethod(L, Lua_RemoveComponent, "RemoveComponent");
            LuaBindings.RegisterMethod(L, Lua_HasComponent, "HasComponent");
            LuaBindings.RegisterMethod(L, Lua_GetField, "GetField");
            LuaBindings.RegisterMethod(L, Lua_SetField, "SetField");
            LuaBindings.RegisterMethod(L, Lua_TryGetField, "TryGetField");
            LuaBindings.RegisterMethod(L, Lua_CreateHost, "CreateHost");
            LuaBindings.RegisterMethod(L, Lua_AttachPrefab, "AttachPrefab");  // <-- DCS-операция
            LuaNative.lua_setglobal(L, "DCS");
        }

        // ------------------------------------------------------------
        //  DCS_CreateHost() -> packedHost
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_CreateHost(IntPtr L)
        {
            Host host = HostManager.CreateHost();
            LuaNative.lua_pushinteger(L, host.ToLua());
            return 1;
        }

        // ------------------------------------------------------------
        //  DCS_Internal_GetTypesCount() -> int
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetTypesCount(IntPtr L)
        {
            LuaNative.lua_pushinteger(L, ComponentRegistry.GetTypesCount());
            return 1;
        }

        // ------------------------------------------------------------
        //  DCS_Internal_GetTypeNameById(id) -> string | nil
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetTypeNameById(IntPtr L)
        {
            int id = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            if (id < 0 || id >= ComponentRegistry.MaxComponentTypes)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            string name = ComponentRegistry.GetTypeNameById(id);
            if (string.IsNullOrEmpty(name))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            LuaNative.lua_pushstring(L, name);
            return 1;
        }

        // ------------------------------------------------------------
        //  DCS_CreateComponent(typeId, packedHost) -> packedHandle | nil
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_CreateComponent(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int packedHost = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes ||
                LuaManager._globalHostChain == null)
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

            Handle handle = pool.SystemAllocate(host, LuaManager._globalHostChain);
            if (handle.IsNull)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            LuaNative.lua_pushinteger(L, handle.Pack());
            return 1;
        }

        // ------------------------------------------------------------
        //  DCS_RemoveComponent(typeId, packedHandle) -> void
        //  Handle already encodes the roster slot, so host lookup
        //  goes through the pool, not through GlobalHosts.
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_RemoveComponent(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            if (packedHandle == HandleConfig.NULL_INDEX ||
                typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes ||
                LuaManager._globalHostChain == null)
                return 0;

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];
            if (pool == null)
                return 0;

            if (pool.TryGetHost(handle, out Host host))
                pool.SystemFree(host, LuaManager._globalHostChain, handle);

            return 0;
        }

        // ------------------------------------------------------------
        //  DCS_HasComponent(typeId, packedHost) -> bool
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_HasComponent(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int packedHost = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes ||
                LuaManager._globalHostChain == null)
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

            ChainNode node = LuaManager._globalHostChain.GetTypedHandle(host, typeId);
            LuaNative.lua_pushboolean(L, node.IsNull ? 0 : 1);
            return 1;
        }

        // ------------------------------------------------------------
        //  DCS_GetField(typeId, packedHandle, fieldName) -> values...
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetField(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            string fieldName = ReadString(L, 3);

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
            {
                return LuaNative.luaL_error(L, $"[DCS Error] Field '{fieldName}' does not exist.");
            }

            int count = LuaNative.lua_gettop(L) - topBefore;
            return count > 0 ? count : 1;
        }

        // ------------------------------------------------------------
        //  DCS_TryGetField(typeId, packedHandle, fieldName) -> ok, values...
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_TryGetField(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            string fieldName = ReadString(L, 3);

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

        // ------------------------------------------------------------
        //  DCS_SetField(typeId, packedHandle, fieldName, values...) -> void
        //  Values are popped from the top of the stack by FieldExpressionFactory.
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_SetField(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            string fieldName = ReadString(L, 3);

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
                    $"[DCS Write Error] Cannot write to field '{fieldName}' on component type {typeId}.");
            }

            return 0;
        }

        private static string ReadString(IntPtr L, int index)
        {
            IntPtr ptr = LuaNative.lua_tolstring(L, index, IntPtr.Zero);
            return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : null;
        }

        // ------------------------------------------------------------
        // Add prefab to the Host ID
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_AttachPrefab(IntPtr L)
        {
            int packedHost = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            string prefabPath = ReadString(L, 2);

            Host host = Host.FromLua(packedHost);
            if (!HostManager.IsValid(host) || string.IsNullOrEmpty(prefabPath))
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            bool ok = ViewService.AttachPrefab(host, prefabPath);
            LuaNative.lua_pushboolean(L, ok ? 1 : 0);
            return 1;
        }
    }
}