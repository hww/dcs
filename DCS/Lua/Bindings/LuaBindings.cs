using System;
using System.Runtime.InteropServices;

namespace DCS.Lua.Bindings
{
    /// <summary>
    /// Single registration entry point for the current binding set.
    /// </summary>
    public static class LuaBindings
    {
        public static void RegisterAll(IntPtr L)
        {
            DcsBindings.Register(L);
            DynamicFactsBinding.Register(L);
            EventBindings.Register(L);
            SpatialBindings.Register(L);
            WorldBindings.Register(L);
            MapBindings.Register(L);
            AIBindings.Register(L);
            InputBindings.Register(L);
        }

        public static void RegisterGlobalFunction(IntPtr L, Func<IntPtr, int> fn, string name)
        {
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(fn);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_setglobal(L, name);
        }

        public static void RegisterMethod(IntPtr L, Func<IntPtr, int> fn, string name)
        {
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(fn);
            LuaNative.lua_pushstring(L, name);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_settable(L, -3);
        }
    }
}
