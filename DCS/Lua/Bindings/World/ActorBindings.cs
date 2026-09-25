using DCS.Core;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DCS.Lua.Bindings
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

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_FindActor(IntPtr L)
        {
            string key = LuaArgumentReader.ReadString(L, 1);
            if (string.IsNullOrEmpty(key))
            {
                LuaNative.lua_pushnil(L);
                return 1;
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

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_GetField(IntPtr L)
        {
            IHostReference reference = HostResolver.ResolveReference(L, 1);
            string field = LuaArgumentReader.ReadString(L, 2);

            if (reference == null || string.IsNullOrEmpty(field))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            if (reference is BaseActor actor)
            {
                int topBefore = LuaNative.lua_gettop(L);
                actor.GetField(field, L);
                int count = LuaNative.lua_gettop(L) - topBefore;
                if (count > 0)
                    return count;
            }

            LuaNative.lua_pushnil(L);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_SetField(IntPtr L)
        {
            IHostReference reference = HostResolver.ResolveReference(L, 1);
            string field = LuaArgumentReader.ReadString(L, 2);

            if (reference is BaseActor actor && !string.IsNullOrEmpty(field))
                actor.SetField(field, L);

            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_GetFact(IntPtr L)
        {
            IHostReference reference = HostResolver.ResolveReference(L, 1);
            string fact = LuaArgumentReader.ReadString(L, 2);

            if (reference is BaseActor actor && !string.IsNullOrEmpty(fact) && actor.GetFact(fact, L))
                return 1;

            LuaNative.lua_pushnil(L);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_SetFact(IntPtr L)
        {
            IHostReference reference = HostResolver.ResolveReference(L, 1);
            string fact = LuaArgumentReader.ReadString(L, 2);

            if (reference is BaseActor actor && !string.IsNullOrEmpty(fact))
                actor.SetFact(fact, L);

            return 0;
        }
    }
}