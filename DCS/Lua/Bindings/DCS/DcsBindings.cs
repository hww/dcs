using DCS.Core;
using System;
using System.Runtime.InteropServices;

namespace DCS.Lua.Bindings
{
    public static class DcsBindings
    {
        public static void Register(IntPtr L)
        {
            LuaBindings.RegisterGlobalFunction(L, Lua_GetTypesCount, "Internal_GetTypesCount");
            LuaBindings.RegisterGlobalFunction(L, Lua_GetTypeNameById, "Internal_GetTypeNameById");

            LuaBindings.RegisterNamespace(L, "DCS", (state, tableIndex) =>
            {
                ComponentBindings.Register(state, tableIndex);
                HostBindings.Register(state, tableIndex);
                EventBindings.Register(state, tableIndex);
            });
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
            int id = LuaArgumentReader.ReadInt(L, 1);

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
    }
}