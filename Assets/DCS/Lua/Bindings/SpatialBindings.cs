using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DynamicComponent.Lua.Bindings
{
    public static class SpatialBindings
    {
        public static void Register(IntPtr L)
        {
            LuaNative.lua_newtable(L);

            LuaNative.lua_pushstring(L, "QueryNearby");
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_QueryNearby);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_settable(L, -3);

            LuaNative.lua_setglobal(L, "Spatial");
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_QueryNearby(IntPtr L)
        {
            float x = (float)LuaNative.lua_tonumberx(L, 1, IntPtr.Zero);
            float y = (float)LuaNative.lua_tonumberx(L, 2, IntPtr.Zero);
            float z = (float)LuaNative.lua_tonumberx(L, 3, IntPtr.Zero);
            float radius = (float)LuaNative.lua_tonumberx(L, 4, IntPtr.Zero);
            EObjectType filterType = (EObjectType)(byte)LuaNative.lua_tointegerx(L, 5, IntPtr.Zero);

            Vector3 queryPos = new Vector3(x, y, z);
            List<ushort> results = new List<ushort>();

            if (SpatialRuntime.Instance != null)
            {
                SpatialRuntime.Instance.GetObjectsInRadius(queryPos, radius, filterType, results);
            }

            LuaNative.lua_newtable(L);
            for (int i = 0; i < results.Count; i++)
            {
                LuaNative.lua_pushinteger(L, i + 1);
                LuaNative.lua_pushinteger(L, results[i]);
                LuaNative.lua_settable(L, -3);
            }
            return 1;
        }
    }
}
