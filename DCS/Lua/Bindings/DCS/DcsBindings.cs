using DCS.Core;
using System;
using System.Runtime.InteropServices;

namespace DCS.Lua.Bindings
{
    /// <summary>
    /// Low-level DCS/ECS Lua API.
    /// Existing global names are intentionally preserved.
    /// </summary>
    public static class DcsBindings
    {
        public static void Register(IntPtr L)
        {
            RegisterGlobalFunction(L, Lua_GetTypesCount, "DCS_Internal_GetTypesCount");
            RegisterGlobalFunction(L, Lua_GetTypeNameById, "DCS_Internal_GetTypeNameById");
            RegisterGlobalFunction(L, Lua_CreateComponent, "DCS_CreateComponent");
            RegisterGlobalFunction(L, Lua_RemoveComponent, "DCS_RemoveComponent");
            RegisterGlobalFunction(L, Lua_HasComponent, "DCS_HasComponent");
            RegisterGlobalFunction(L, Lua_GetField, "DCS_GetField");
            RegisterGlobalFunction(L, Lua_SetField, "DCS_SetField");
            RegisterGlobalFunction(L, Lua_TryGetField, "DCS_TryGetField");
            RegisterGlobalFunction(L, Lua_CreateHost, "DCS_CreateHost");
        }

        private static void RegisterGlobalFunction(IntPtr L, Func<IntPtr, int> fn, string name)
        {
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(fn);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_setglobal(L, name);
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_CreateHost(IntPtr L)
        {
            Host host = HostManager.CreateHost();
            LuaNative.lua_pushinteger(L, host.Pack());
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetTypesCount(IntPtr L)
        {
            LuaNative.lua_pushinteger(L, ComponentRegistry.GetTypesCount());
            return 1;
        }

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
            if (name == null)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            LuaNative.lua_pushstring(L, name);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_CreateComponent(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int hostId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes ||
                hostId < 0 || hostId >= HostManager.GlobalHosts.Length ||
                LuaManager._globalHostChain == null)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            ref HostData hostData = ref HostManager.GlobalHosts[hostId];
            Host host = new Host { Id = (ushort)hostId, Generation = hostData.Generation };
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

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_HasComponent(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int hostId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes ||
                hostId < 0 || hostId >= HostManager.GlobalHosts.Length ||
                LuaManager._globalHostChain == null)
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            ref HostData hostData = ref HostManager.GlobalHosts[hostId];
            Host host = new Host { Id = (ushort)hostId, Generation = hostData.Generation };
            if (!HostManager.IsValid(host))
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            ChainNode node = LuaManager._globalHostChain.GetTypedHandle(host, typeId);
            LuaNative.lua_pushboolean(L, node.IsNull ? 0 : 1);
            return 1;
        }

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
    }
}
