using System;
using System.Runtime.InteropServices;

namespace DCS.Lua.Bindings
{
    public static class LuaBindings
    {
        public static void RegisterAll(IntPtr L)
        {
            DcsBindings.Register(L);
            DynamicFactsBinding.Register(L);
            SpatialBindings.Register(L);
            WorldBindings.Register(L);
            MapBindings.Register(L);
            AIBindings.Register(L);
            InputBindings.Register(L);
            CameraBindings.Register(L);
            SceneBindings.Register(L);
            VectorBindings.Register(L);         
            LuaRuntimeBindings.Register(L);     
        }

        public static void RegisterGlobalFunction(IntPtr L, Func<IntPtr, int> fn, string name)
        {
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(fn);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_setglobal(L, name);
        }

        /// <summary>
        /// Registers a method into the table at the given stack index.
        /// Leaves the table untouched.
        /// </summary>
        public static void RegisterMethod(IntPtr L, Func<IntPtr, int> fn, int tableIndex, string name)
        {
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(fn);
            LuaNative.lua_pushstring(L, name);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_settable(L, tableIndex);
        }

        /// <summary>
        /// Creates a new table, invokes the filler, and assigns it to a global.
        /// </summary>
        public static void RegisterNamespace(IntPtr L, string globalName, Action<IntPtr, int> fillTable)
        {
            LuaNative.lua_newtable(L);
            int tableIndex = LuaNative.lua_gettop(L);
            fillTable(L, tableIndex);
            LuaNative.lua_setglobal(L, globalName);
        }
    }
}