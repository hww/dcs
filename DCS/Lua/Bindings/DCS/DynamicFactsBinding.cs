using DCS.Actors;
using DCS.Core;
using System;
using System.Runtime.InteropServices;

namespace DCS.Lua
{
    public static class DynamicFactsBinding
    {
        public static void Register(IntPtr L)
        {
            LuaBindings.RegisterGlobalFunction(L, Lua_GetFact, "facts_get");
            LuaBindings.RegisterGlobalFunction(L, Lua_TryGetFact, "facts_try_get");
            LuaBindings.RegisterGlobalFunction(L, Lua_SetFact, "facts_set");
        }

        /// <summary>
        /// Извлекает DynamicFacts из userdata. Бросает Lua-ошибку,
        /// если аргумент не userdata или GCHandle невалидный.
        /// </summary>
        private static DynamicFacts GetFacts(IntPtr L, ArgReader args, string funcName)
        {
            IntPtr ptr = args.CheckUserdataRaw(1);
            if (ptr == IntPtr.Zero)
                LuaFail.Fail(L, funcName, "argument #1: invalid userdata pointer");

            GCHandle handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(ptr));
            if (!handle.IsAllocated)
                LuaFail.Fail(L, funcName, "argument #1: invalid DynamicFacts handle");

            DynamicFacts facts = handle.Target as DynamicFacts;
            if (facts == null)
                LuaFail.Fail(L, funcName, "argument #1: not a DynamicFacts object");

            return facts;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetFact(IntPtr L)
        {
            var args = new ArgReader(L, "facts_get");
            args.ExpectExactly(2);

            DynamicFacts facts = GetFacts(L, args, "facts_get");
            string factName = args.CheckString(2);

            if (string.IsNullOrEmpty(factName))
                LuaFail.Fail(L, "facts_get", "argument #2: fact name must not be empty");

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
            var args = new ArgReader(L, "facts_try_get");
            args.ExpectExactly(2);

            DynamicFacts facts = GetFacts(L, args, "facts_try_get");
            string factName = args.CheckString(2);

            if (string.IsNullOrEmpty(factName))
                LuaFail.Fail(L, "facts_try_get", "argument #2: fact name must not be empty");

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
            var args = new ArgReader(L, "facts_set");
            args.ExpectExactly(3);

            DynamicFacts facts = GetFacts(L, args, "facts_set");
            string factName = args.CheckString(2);
            args.CheckAny(3);   // значение — любой тип

            if (string.IsNullOrEmpty(factName))
                LuaFail.Fail(L, "facts_set", "argument #2: fact name must not be empty");

            facts.SetFact(factName, L);
            return 0;
        }
    }
}