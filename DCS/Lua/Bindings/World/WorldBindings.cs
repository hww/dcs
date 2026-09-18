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
            LuaBindings.RegisterMethod(L, ActorBindings.Lua_FindActor, "FindActor");
            LuaBindings.RegisterMethod(L, ActorBindings.Lua_GetField, "GetField");
            LuaBindings.RegisterMethod(L, ActorBindings.Lua_SetField, "SetField");
            LuaBindings.RegisterMethod(L, ActorBindings.Lua_GetFact, "GetFact");
            LuaBindings.RegisterMethod(L, ActorBindings.Lua_SetFact, "SetFact");
            LuaNative.lua_setglobal(L, "World");
        }
    }
}
