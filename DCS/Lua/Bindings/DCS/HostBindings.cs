using DCS.Actors;
using DCS.Core;
using DCS.World;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DCS.Lua
{
    public static class HostBindings
    {
        public static void Register(IntPtr L, int tableIndex)
        {
            LuaBindings.RegisterMethod(L, Lua_CreateHost, tableIndex, "CreateHost");
            LuaBindings.RegisterMethod(L, Lua_Attach, tableIndex, "Attach");
            LuaBindings.RegisterMethod(L, Lua_Spawn, tableIndex, "Spawn");
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_CreateHost(IntPtr L)
        {
            var args = new ArgReader(L, "CreateHost");
            args.ExpectExactly(0);

            Host host = HostManager.CreateHost();
            LuaNative.lua_pushinteger(L, host.ToLua());
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Attach(IntPtr L)
        {
            var args = new ArgReader(L, "Attach");
            args.ExpectExactly(2);

            int packedHost = (int)args.CheckInteger(1);
            string goName = args.CheckString(2);

            if (string.IsNullOrEmpty(goName))
                LuaFail.Fail(L, "Attach", "argument #2: game object name must not be empty");

            Host host = Host.FromLua(packedHost);
            if (!HostManager.IsValid(host))
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            bool ok = ViewService.Attach(host, goName);
            LuaNative.lua_pushboolean(L, ok ? 1 : 0);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Spawn(IntPtr L)
        {
            var args = new ArgReader(L, "DCS.Spawn");
            args.ExpectInRange(2, 6);

            string prefabPath = args.CheckString(1);
            string goName = args.CheckString(2);

            float x = 0f, y = 0f, z = 0f;
            if (args.Count >= 5)
            {
                x = (float)args.CheckNumber(3);
                y = (float)args.CheckNumber(4);
                z = (float)args.CheckNumber(5);
            }

            bool callBirth = false;
            if (args.Count >= 6)
                callBirth = args.CheckBool(6);

            if (string.IsNullOrEmpty(prefabPath))
                LuaFail.Fail(L, "DCS.Spawn", "argument #1: prefab path must not be empty");
            if (string.IsNullOrEmpty(goName))
                LuaFail.Fail(L, "DCS.Spawn", "argument #2: game object name must not be empty");

            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            Vector3 pos = new Vector3(x, y, z);
            GameObject go = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
            go.name = goName;

            var link = go.GetComponentInChildren<IHostReference>();
            if (link == null)
            {
                UnityEngine.Object.Destroy(go);
                LuaNative.lua_pushnil(L);
                return 1;
            }

            Host host = HostManager.CreateHost();
            HostManager.LinkHostReference(host, link);

            if (callBirth && link is ILifeCycle lc)
                lc.Birth();

            LuaNative.lua_pushinteger(L, host.ToLua());
            return 1;
        }
    }
}