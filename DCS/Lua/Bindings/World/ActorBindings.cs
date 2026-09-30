using DCS.Actors;
using DCS.Core;
using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace DCS.Lua
{
    public static class ActorBindings
    {
        public static void Register(IntPtr L, int tableIndex)
        {
            LuaBindings.RegisterMethod(L, Lua_FindActor, tableIndex, "FindActor");
            LuaBindings.RegisterMethod(L, Lua_GetField, tableIndex, "GetField");
            LuaBindings.RegisterMethod(L, Lua_SetField, tableIndex, "SetField");
            LuaBindings.RegisterMethod(L, Lua_GetFact, tableIndex, "GetFact");
            LuaBindings.RegisterMethod(L, Lua_SetFact, tableIndex, "SetFact");
        }

        // ============================================================
        //  FindActor(name) -> hostPacked | nil
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_FindActor(IntPtr L)
        {
            var args = new ArgReader(L, "World.FindActor");
            args.ExpectExactly(1);

            string key = args.CheckString(1);
            if (string.IsNullOrEmpty(key))
            {
                LuaFail.Fail(L, "World.FindActor", "argument #1: name must not be empty");
                return 0;
            }

            GameObject go = GameObject.Find(key);
            if (go == null)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            IHostReference reference = go.GetComponent<IHostReference>();
            if (reference == null || !HostManager.IsValid(reference.Host))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            LuaNative.lua_pushinteger(L, reference.Host.ToLua());
            return 1;
        }

        // ============================================================
        //  GetField(actor, "field") -> value | nil
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_GetField(IntPtr L)
        {
            var args = new ArgReader(L, "World.GetField");
            args.ExpectExactly(2);

            // ---- argument #1: packed Host (integer) ----
            int packedHost = (int)args.CheckInteger(1);

            if (packedHost == 0)
            {
                LuaFail.Fail(L, "World.GetField",
                    "argument #1: host id must not be zero");
                return 0;
            }

            Host host = Host.FromLua(packedHost);
            if (!HostManager.IsValid(host))
            {
                LuaFail.Fail(L, "World.GetField",
                    $"argument #1: host (id={host.Id}, gen={host.Generation}) is not valid");
                return 0;
            }

            // ---- resolve actor ----
            IHostReference reference = HostManager.GetActor(host);
            if (reference == null)
            {
                LuaFail.Fail(L, "World.GetField",
                    $"argument #1: host (id={host.Id}, gen={host.Generation}) has no actor linked");
                return 0;
            }

            // ---- argument #2: field name ----
            string field = args.CheckString(2);
            if (string.IsNullOrEmpty(field))
            {
                LuaFail.Fail(L, "World.GetField",
                    "argument #2: field name must not be empty");
                return 0;
            }

            // ---- actor must support fields ----
            BaseActor actor = reference as BaseActor;
            if (actor == null)
            {
                LuaFail.Fail(L, "World.GetField",
                    $"argument #1: actor type {reference.GetType().Name} has no fields");
                return 0;
            }

            // ---- call ----
            int topBefore = LuaNative.lua_gettop(L);
            actor.GetField(field, L);
            int count = LuaNative.lua_gettop(L) - topBefore;
            if (count > 0)
                return count;

            LuaNative.lua_pushnil(L);
            return 1;
        }

        // ============================================================
        //  SetField(actor, "field", value)
        // ============================================================


        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_SetField(IntPtr L)
        {
            var args = new ArgReader(L, "World.SetField");
            args.ExpectExactly(3);

            // ---- argument #1: packed Host (integer) ----
            int packedHost = (int)args.CheckInteger(1);

            if (packedHost == 0)
            {
                LuaFail.Fail(L, "World.SetField",
                    "argument #1: host id must not be zero");
                return 0;
            }

            Host host = Host.FromLua(packedHost);
            if (!HostManager.IsValid(host))
            {
                LuaFail.Fail(L, "World.SetField",
                    $"argument #1: host (id={host.Id}, gen={host.Generation}) is not valid");
                return 0;
            }

            // ---- resolve actor ----
            IHostReference reference = HostManager.GetActor(host);
            if (reference == null)
            {
                LuaFail.Fail(L, "World.SetField",
                    $"argument #1: host (id={host.Id}, gen={host.Generation}) has no actor linked");
                return 0;
            }

            // ---- argument #2: field name ----
            string field = args.CheckString(2);
            if (string.IsNullOrEmpty(field))
            {
                LuaFail.Fail(L, "World.SetField",
                    "argument #2: field name must not be empty");
                return 0;
            }

            // ---- argument #3: value (any type) ----
            args.CheckAny(3);

            // ---- actor must support fields ----
            BaseActor actor = reference as BaseActor;
            if (actor == null)
            {
                LuaFail.Fail(L, "World.SetField",
                    $"argument #1: actor type {reference.GetType().Name} has no fields");
                return 0;
            }

            // ---- call ----
            actor.SetField(field, L);
            return 0;
        }


        // ============================================================
        //  GetFact(actor, "fact") -> value | nil
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_GetFact(IntPtr L)
        {
            var args = new ArgReader(L, "World.GetFact");
            args.ExpectExactly(2);

            IHostReference reference = HostResolver.ResolveReference(L, 1);
            if (reference == null)
            {
                LuaFail.Fail(L, "World.GetFact",
                    "argument #1: expected host reference userdata");
                return 0;
            }

            string fact = args.CheckString(2);
            if (string.IsNullOrEmpty(fact))
            {
                LuaFail.Fail(L, "World.GetFact",
                    "argument #2: fact name must not be empty");
                return 0;
            }

            BaseActor actor = reference as BaseActor;
            if (actor == null)
            {
                LuaFail.Fail(L, "World.GetFact",
                    $"argument #1: host object has no facts (got {reference.GetType().Name})");
                return 0;
            }

            if (actor.GetFact(fact, L))
                return 1;

            LuaNative.lua_pushnil(L);
            return 1;
        }

        // ============================================================
        //  SetFact(actor, "fact", value)
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_SetFact(IntPtr L)
        {
            var args = new ArgReader(L, "World.SetFact");
            args.ExpectExactly(3);

            IHostReference reference = HostResolver.ResolveReference(L, 1);
            if (reference == null)
            {
                LuaFail.Fail(L, "World.SetFact",
                    "argument #1: expected host reference userdata");
                return 0;
            }

            string fact = args.CheckString(2);
            if (string.IsNullOrEmpty(fact))
            {
                LuaFail.Fail(L, "World.SetFact",
                    "argument #2: fact name must not be empty");
                return 0;
            }

            args.CheckAny(3);

            BaseActor actor = reference as BaseActor;
            if (actor == null)
            {
                LuaFail.Fail(L, "World.SetFact",
                    $"argument #1: host object has no facts (got {reference.GetType().Name})");
                return 0;
            }

            actor.SetFact(fact, L);
            return 0;
        }
    }
}