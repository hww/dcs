using System;
using DCS.Core;
using DCS.Game.SoldierLua;
using UnityEngine;

namespace DCS.Lua
{
    public static class SoldierBindings
    {
        public static void Register(IntPtr L)
        {
            LuaBindings.RegisterNamespace(L, "Soldier", (state, tableIndex) =>
            {
                LuaBindings.RegisterMethod(state, Lua_Spawn, tableIndex, "Spawn");
            });
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Spawn(IntPtr L)
        {
            var args = new ArgReader(L, "Soldier.Spawn");
            args.ExpectExactly(6);
            args.CheckInteger(1);
            string prefabPath = args.CheckString(2);
            string soldierName = args.CheckString(3);
            float x = (float)args.CheckNumber(4);
            float y = (float)args.CheckNumber(5);
            float z = (float)args.CheckNumber(6);

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            Host host = SoldierFactory.Spawn(
                domain, prefabPath, soldierName, new Vector3(x, y, z));

            if (host.IsNull)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            LuaNative.lua_pushinteger(L, host.ToLua());
            return 1;
        }
    }
}