using DynamicComponent.Lua;
using System;
using System.Runtime.InteropServices;

namespace DynamicComponent
{
    /// <summary>
    /// Биндинги для DynamicFacts в Lua.
    /// </summary>
    public static class DynamicFactsLuaBinding
    {
        public static void Register(IntPtr L)
        {
            // Как в LuaManager - используем Func<IntPtr, int>
            RegisterLuaFunction(L, Lua_GetFact, "facts_get");
            RegisterLuaFunction(L, Lua_TryGetFact, "facts_try_get");
            RegisterLuaFunction(L, Lua_SetFact, "facts_set");
        }

        private static void RegisterLuaFunction(IntPtr L, Func<IntPtr, int> fn, string name)
        {
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(fn);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_setglobal(L, name);
        }

        private static int Lua_GetFact(IntPtr L)
        {
            if (LuaNative.lua_type(L, 1) != LuaNative.LUA_TUSERDATA)
                return LuaNative.luaL_error(L, "Expected DynamicFacts as first argument");

            IntPtr ptr = LuaNative.lua_touserdata(L, 1);
            if (ptr == IntPtr.Zero)
                return LuaNative.luaL_error(L, "Invalid userdata");

            GCHandle handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(ptr));
            var facts = handle.Target as DynamicFacts;
            if (facts == null)
                return LuaNative.luaL_error(L, "Invalid DynamicFacts object");

            IntPtr strPtr = LuaNative.lua_tolstring(L, 2, IntPtr.Zero);
            string fieldName = strPtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(strPtr) : null;

            if (string.IsNullOrEmpty(fieldName))
                return LuaNative.luaL_error(L, "Field name required");

            facts.GetFact(fieldName, L);
            return 1;
        }

        private static int Lua_TryGetFact(IntPtr L)
        {
            if (LuaNative.lua_type(L, 1) != LuaNative.LUA_TUSERDATA)
                return LuaNative.luaL_error(L, "Expected DynamicFacts as first argument");

            IntPtr ptr = LuaNative.lua_touserdata(L, 1);
            if (ptr == IntPtr.Zero)
                return LuaNative.luaL_error(L, "Invalid userdata");

            GCHandle handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(ptr));
            var facts = handle.Target as DynamicFacts;
            if (facts == null)
                return LuaNative.luaL_error(L, "Invalid DynamicFacts object");

            IntPtr strPtr = LuaNative.lua_tolstring(L, 2, IntPtr.Zero);
            string fieldName = strPtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(strPtr) : null;

            if (string.IsNullOrEmpty(fieldName))
                return LuaNative.luaL_error(L, "Field name required");

            bool hasFact = facts.Contains(fieldName);
            facts.GetFact(fieldName, L);
            LuaNative.lua_pushboolean(L, hasFact ? 1 : 0);
            LuaNative.lua_rotate(L, -2, 1);

            return 2;
        }

        private static int Lua_SetFact(IntPtr L)
        {
            if (LuaNative.lua_type(L, 1) != LuaNative.LUA_TUSERDATA)
                return LuaNative.luaL_error(L, "Expected DynamicFacts as first argument");

            IntPtr ptr = LuaNative.lua_touserdata(L, 1);
            if (ptr == IntPtr.Zero)
                return LuaNative.luaL_error(L, "Invalid userdata");

            GCHandle handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(ptr));
            var facts = handle.Target as DynamicFacts;
            if (facts == null)
                return LuaNative.luaL_error(L, "Invalid DynamicFacts object");

            IntPtr strPtr = LuaNative.lua_tolstring(L, 2, IntPtr.Zero);
            string fieldName = strPtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(strPtr) : null;

            if (string.IsNullOrEmpty(fieldName))
                return LuaNative.luaL_error(L, "Field name required");

            facts.SetFact(fieldName, L);
            return 0;
        }
    }
}