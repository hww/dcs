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
        // ------------------------------------------------------------
        //  World.FindActor(key) -> packedHost | nil
        //  Запрос: не найденный объект — валидный nil.
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_FindActor(IntPtr L)
        {
            string key = ReadString(L, 1);
            if (string.IsNullOrEmpty(key))
                return LuaNative.lua_error(L,
                    "[ActorBindings] FindActor: actor key is empty");

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

        // ------------------------------------------------------------
        //  World.GetField(packedHost, field) -> values... | nil
        //  Запрос: нет поля / нет ссылки — nil, без ошибки.
        // ------------------------------------------------------------
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

        // ------------------------------------------------------------
        //  World.SetField(packedHost, field, values...) -> void
        //  Команда: null ref / пустое поле / не-BaseActor — ошибка.
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_SetField(IntPtr L)
        {
            IHostReference reference = ResolveReference(L, 1);
            if (reference == null)
                return LuaNative.lua_error(L,
                    "[ActorBindings] SetField: invalid or stale actor reference");

            string field = ReadString(L, 2);
            if (string.IsNullOrEmpty(field))
                return LuaNative.lua_error(L,
                    "[ActorBindings] SetField: field name is empty");

            if (!(reference is BaseActor actor))
                return LuaNative.lua_error(L,
                    $"[ActorBindings] SetField: reference is not a BaseActor ({reference.GetType().Name})");

            actor.SetField(field, L);
            return 0;
        }

        // ------------------------------------------------------------
        //  World.GetFact(packedHost, fact) -> value | nil
        //  Запрос: нет факта — nil. Null ref / пустое имя — ошибка.
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_GetFact(IntPtr L)
        {
            IHostReference reference = ResolveReference(L, 1);
            if (reference == null)
                return LuaNative.lua_error(L,
                    "[ActorBindings] GetFact: invalid or stale actor reference");

            string fact = ReadString(L, 2);
            if (string.IsNullOrEmpty(fact))
                return LuaNative.lua_error(L,
                    "[ActorBindings] GetFact: fact name is empty");

            if (reference is BaseActor actor && actor.GetFact(fact, L))
                return 1;

            LuaNative.lua_pushnil(L);
            return 1;
        }

        // ------------------------------------------------------------
        //  World.SetFact(packedHost, fact, value) -> void
        //  Команда: null ref / пустое имя — ошибка.
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_SetFact(IntPtr L)
        {
            IHostReference reference = ResolveReference(L, 1);
            if (reference == null)
                return LuaNative.lua_error(L,
                    "[ActorBindings] SetFact: invalid or stale actor reference");

            string fact = ReadString(L, 2);
            if (string.IsNullOrEmpty(fact))
                return LuaNative.lua_error(L,
                    "[ActorBindings] SetFact: fact name is empty");

            if (!(reference is BaseActor actor))
                return LuaNative.lua_error(L,
                    $"[ActorBindings] SetFact: reference is not a BaseActor ({reference.GetType().Name})");

            actor.SetFact(fact, L);
            return 0;
        }

        private static IHostReference ResolveReference(IntPtr L, int index)
        {
            long packedRaw = LuaNative.lua_tointegerx(L, index, IntPtr.Zero);
            if (packedRaw < 0 || packedRaw == HandleConfig.NULL_INDEX)
                return null;

            Handle handle = new Handle((int)packedRaw);
            return HostManager.GetHostReference(handle);
        }

        private static string ReadString(IntPtr L, int index)
        {
            IntPtr ptr = LuaNative.lua_tolstring(L, index, IntPtr.Zero);
            return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : null;
        }
    }
}