using System;
using System.Runtime.InteropServices;
using UnityEngine;
using DCS.Core;

namespace DCS.Lua.Bindings
{
    /// <summary>
    /// World/scene access for actor references.
    /// Name lookup is intentionally a cold-path operation: the returned value is a packed Host handle.
    /// </summary>
    public static class ActorBindings
    {
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_FindActor(IntPtr L)
        {
            string key = ReadString(L, 1);
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
            IHostReference reference = ResolveReference(L, 1);
            string field = ReadString(L, 2);

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
            IHostReference reference = ResolveReference(L, 1);
            string field = ReadString(L, 2);

            if (reference is BaseActor actor && !string.IsNullOrEmpty(field))
                actor.SetField(field, L);

            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_GetFact(IntPtr L)
        {
            IHostReference reference = ResolveReference(L, 1);
            string fact = ReadString(L, 2);

            if (reference is BaseActor actor && !string.IsNullOrEmpty(fact) && actor.GetFact(fact, L))
                return 1;

            LuaNative.lua_pushnil(L);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_SetFact(IntPtr L)
        {
            IHostReference reference = ResolveReference(L, 1);
            string fact = ReadString(L, 2);

            if (reference is BaseActor actor && !string.IsNullOrEmpty(fact))
                actor.SetFact(fact, L);

            return 0;
        }

        private static IHostReference ResolveReference(IntPtr L, int index)
        {
            int packed = (int)LuaNative.lua_tointegerx(L, index, IntPtr.Zero);
            if (packed == HandleConfig.NULL_INDEX)
                return null;

            Handle handle = new Handle(packed);
            return HostManager.GetHostReference(handle);
        }

        private static string ReadString(IntPtr L, int index)
        {
            IntPtr ptr = LuaNative.lua_tolstring(L, index, IntPtr.Zero);
            return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : null;
        }
    }
}
