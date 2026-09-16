using DCS.Core;
using System;
using System.Runtime.InteropServices;

namespace DCS.Lua.Bindings
{
    /// <summary>
    /// Lua access to DynamicFacts userdata.
    /// Existing global function names are preserved.
    /// </summary>
    public static class DynamicFactsBinding
    {
        public static void Register(IntPtr L)
        {
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

        private static DynamicFacts GetFacts(IntPtr L)
        {
            if (LuaNative.lua_type(L, 1) != LuaNative.LUA_TUSERDATA)
            {
                LuaNative.luaL_error(L, "Expected DynamicFacts as first argument");
                return null;
            }

            IntPtr ptr = LuaNative.lua_touserdata(L, 1);
            if (ptr == IntPtr.Zero)
            {
                LuaNative.luaL_error(L, "Invalid userdata");
                return null;
            }

            GCHandle handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(ptr));
            if (!handle.IsAllocated)
            {
                LuaNative.luaL_error(L, "Invalid DynamicFacts handle");
                return null;
            }

            DynamicFacts facts = handle.Target as DynamicFacts;
            if (facts == null)
            {
                LuaNative.luaL_error(L, "Invalid DynamicFacts object");
                return null;
            }

            return facts;
        }

        private static string GetFactName(IntPtr L)
        {
            IntPtr strPtr = LuaNative.lua_tolstring(L, 2, IntPtr.Zero);
            return strPtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(strPtr) : null;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetFact(IntPtr L)
        {
            DynamicFacts facts = GetFacts(L);
            if (facts == null)
                return 0;

            string factName = GetFactName(L);
            if (string.IsNullOrEmpty(factName))
                return LuaNative.luaL_error(L, "Fact name required");

            if (!facts.GetFact(factName, L))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_TryGetFact(IntPtr L)
        {
            DynamicFacts facts = GetFacts(L);
            if (facts == null)
                return 0;

            string factName = GetFactName(L);
            if (string.IsNullOrEmpty(factName))
                return LuaNative.luaL_error(L, "Fact name required");

            bool exists = facts.Contains(factName);
            bool pushed = facts.GetFact(factName, L);

            if (!pushed)
                LuaNative.lua_pushnil(L);

            LuaNative.lua_pushboolean(L, exists ? 1 : 0);
            LuaNative.lua_rotate(L, -2, 1);
            return 2;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_SetFact(IntPtr L)
        {
            DynamicFacts facts = GetFacts(L);
            if (facts == null)
                return 0;

            string factName = GetFactName(L);
            if (string.IsNullOrEmpty(factName))
                return LuaNative.luaL_error(L, "Fact name required");

            facts.SetFact(factName, L);
            return 0;
        }
    }
}
