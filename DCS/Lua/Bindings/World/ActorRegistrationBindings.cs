using DCS.Core;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DCS.Lua.Bindings
{
    public static class ActorRegistrationBindings
    {
        public static void Register(IntPtr L, int tableIndex)
        {
            LuaBindings.RegisterMethod(L, Lua_RegisterActor, tableIndex, "RegisterActor");
            LuaBindings.RegisterMethod(L, Lua_UnregisterActor, tableIndex, "UnregisterActor");
            LuaBindings.RegisterMethod(L, Lua_GetActorHost, tableIndex, "GetActorHost");
            LuaBindings.RegisterMethod(L, Lua_IsActorRegistered, tableIndex, "IsActorRegistered");
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_RegisterActor(IntPtr L)
        {
            int packedHost = LuaArgumentReader.ReadInt(L, 1);
            string goName = LuaArgumentReader.ReadString(L, 2);

            if (string.IsNullOrEmpty(goName))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            Host host = Host.FromLua(packedHost);
            if (!HostManager.IsValid(host))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            GameObject go = GameObject.Find(goName);
            if (go == null)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            var link = go.GetComponent<IHostReference>();
            if (link == null)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            HostManager.LinkHostReference(host, link);

            LuaNative.lua_pushboolean(L, 1);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_UnregisterActor(IntPtr L)
        {
            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
                return 0;

            int packedHost = LuaArgumentReader.ReadInt(L, 2);

            Host host = Host.FromLua(packedHost);
            if (!HostManager.IsValid(host))
                return 0;

            // Frees all components and invalidates the host.
            domain.HostChain.FreeChain(host);

            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetActorHost(IntPtr L)
        {
            string goName = LuaArgumentReader.ReadString(L, 1);
            if (string.IsNullOrEmpty(goName))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            GameObject go = GameObject.Find(goName);
            if (go == null)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            var link = go.GetComponent<IHostReference>();
            if (link == null || !HostManager.IsValid(link.Host))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            LuaNative.lua_pushinteger(L, link.Host.ToLua());
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_IsActorRegistered(IntPtr L)
        {
            string goName = LuaArgumentReader.ReadString(L, 1);
            if (string.IsNullOrEmpty(goName))
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            GameObject go = GameObject.Find(goName);
            if (go == null)
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            var link = go.GetComponent<IHostReference>();
            bool registered = link != null && HostManager.IsValid(link.Host);
            LuaNative.lua_pushboolean(L, registered ? 1 : 0);
            return 1;
        }
    }
}