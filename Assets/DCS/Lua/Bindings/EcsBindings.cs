using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DynamicComponent.Lua.Bindings
{
    public static class EcsBindings
    {
        public static void Register(IntPtr L)
        {
            // Регистрируем функции в глобальную область Lua, как это было изначально
            RegisterGlobalFunction(L, Lua_GetTypesCount, "DCS_Internal_GetTypesCount");
            RegisterGlobalFunction(L, Lua_GetTypeNameById, "DCS_Internal_GetTypeNameById");
            RegisterGlobalFunction(L, Lua_CreateComponent, "DCS_CreateComponent");
            RegisterGlobalFunction(L, Lua_RemoveComponent, "DCS_RemoveComponent");
            RegisterGlobalFunction(L, Lua_HasComponent, "DCS_HasComponent");
            RegisterGlobalFunction(L, Lua_GetField, "DCS_GetField");
            RegisterGlobalFunction(L, Lua_SetField, "DCS_SetField");
            RegisterGlobalFunction(L, Lua_TryGetField, "DCS_TryGetField");
        }

        private static void RegisterGlobalFunction(IntPtr L, Func<IntPtr, int> fn, string name)
        {
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(fn);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_setglobal(L, name);
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
            string name = ComponentRegistry.GetTypeNameById(id);
            LuaNative.lua_pushstring(L, name);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_CreateComponent(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int hostId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            if (hostId < 0 || hostId >= HostManager.GlobalHosts.Length)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            ref HostData hostData = ref HostManager.GlobalHosts[hostId];
            Host host = new Host { Id = (ushort)hostId, Generation = hostData.Generation };

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            var pool = ComponentRegistry.Pools[typeId];
            if (pool != null && LuaManager._globalHostChain != null) // Доступ через внутреннее связывание
            {
                Handle handle = pool.SystemAllocate(host, LuaManager._globalHostChain);
                LuaNative.lua_pushinteger(L, handle.Pack());
                return 1;
            }

            LuaNative.lua_pushnil(L);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_RemoveComponent(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            if (packedHandle == HandleConfig.NULL_INDEX) return 0;

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];

            if (pool != null && LuaManager._globalHostChain != null)
            {
                if (pool.TryGetHost(handle, out Host host))
                {
                    pool.SystemFree(host, LuaManager._globalHostChain, handle);
                }
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_HasComponent(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int hostId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);

            if (hostId < 0 || hostId >= HostManager.GlobalHosts.Length)
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            ref HostData hostData = ref HostManager.GlobalHosts[hostId];
            Host host = new Host { Id = (ushort)hostId, Generation = hostData.Generation };

            bool has = false;
            if (LuaManager._globalHostChain != null)
            {
                ChainNode node = LuaManager._globalHostChain.GetTypedHandle(host, typeId);
                has = !node.IsNull;
            }

            LuaNative.lua_pushboolean(L, has);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetField(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            IntPtr ptr = LuaNative.lua_tolstring(L, 3, IntPtr.Zero);

            string fieldName = ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : null;

            if (packedHandle == HandleConfig.NULL_INDEX || string.IsNullOrEmpty(fieldName))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];

            if (pool != null && pool.TryGetDenseIndex(handle, out int denseIndex))
            {
                bool success = pool.GetField(denseIndex, fieldName, L);
                if (!success)
                {
                    return LuaNative.luaL_error(L, $"[DCS Error] Field '{fieldName}' does not exist on component type {typeId} for Handle {packedHandle}");
                }

                return fieldName.Equals("position", StringComparison.OrdinalIgnoreCase) ||
                       fieldName.Equals("rotation", StringComparison.OrdinalIgnoreCase) ? 3 : 1;
            }

            LuaNative.lua_pushnil(L);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_TryGetField(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            IntPtr ptr = LuaNative.lua_tolstring(L, 3, IntPtr.Zero);
            string fieldName = ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : null;

            if (packedHandle == HandleConfig.NULL_INDEX || string.IsNullOrEmpty(fieldName))
            {
                LuaNative.lua_pushboolean(L, 0);
                LuaNative.lua_pushnil(L);
                return 2;
            }

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];

            if (pool != null && pool.TryGetDenseIndex(handle, out int denseIndex))
            {
                bool success = pool.GetField(denseIndex, fieldName, L);
                if (success)
                {
                    LuaNative.lua_pushboolean(L, 1);
                    int valueSize = fieldName.Equals("position", StringComparison.OrdinalIgnoreCase) ||
                                    fieldName.Equals("rotation", StringComparison.OrdinalIgnoreCase) ? 3 : 1;

                    LuaNative.lua_rotate(L, -(valueSize + 1), 1);
                    return valueSize + 1;
                }
            }

            LuaNative.lua_pushboolean(L, 0);
            LuaNative.lua_pushnil(L);
            return 2;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_SetField(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            IntPtr ptr = LuaNative.lua_tolstring(L, 3, IntPtr.Zero);
            string fieldName = ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : null;

            if (packedHandle == HandleConfig.NULL_INDEX || string.IsNullOrEmpty(fieldName))
                return 0;

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];

            if (pool != null && pool.TryGetDenseIndex(handle, out int denseIndex))
            {
                bool success = pool.SetField(denseIndex, fieldName, L);
                if (!success)
                {
                    return LuaNative.luaL_error(L, $"[DCS Write Error] Cannot write to non-existent field '{fieldName}' on component type {typeId}");
                }
            }
            return 0;
        }
    }
}
