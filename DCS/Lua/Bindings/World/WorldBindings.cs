using System;
using System.Runtime.InteropServices;

namespace DCS.Lua.Bindings
{
    /// <summary>High-level world facade for Unity-backed gameplay objects.</summary>
    public static class WorldBindings
    {
        public static void Register(IntPtr L)
        {
            LuaNative.lua_newtable(L);
            RegisterMethod(L, "FindActor", ActorBindings.Lua_FindActor);
            RegisterMethod(L, "GetField", ActorBindings.Lua_GetField);
            RegisterMethod(L, "SetField", ActorBindings.Lua_SetField);
            RegisterMethod(L, "GetFact", ActorBindings.Lua_GetFact);
            RegisterMethod(L, "SetFact", ActorBindings.Lua_SetFact);
            LuaNative.lua_setglobal(L, "World");
        }

        private static void RegisterMethod(IntPtr L, string name, Func<IntPtr, int> fn)
        {
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(fn);
            LuaNative.lua_pushstring(L, name);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_settable(L, -3);
        }
    }
}
