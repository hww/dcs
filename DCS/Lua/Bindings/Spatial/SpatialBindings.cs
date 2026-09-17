using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DCS.Core;
using DCS.Spatial;
using UnityEngine;

namespace DCS.Lua.Bindings
{
    public static class SpatialBindings
    {
        private static readonly List<ushort> _results = new List<ushort>(64);

        public static void Register(IntPtr L)
        {
            LuaNative.lua_newtable(L);
            RegisterMethod(L, "QueryRadius", Lua_QueryRadius);
            RegisterMethod(L, "QueryPoint", Lua_QueryPoint);
            RegisterMethod(L, "Contains", Lua_Contains);
            RegisterMethod(L, "Raycast", Lua_Raycast);      // <-- NEW
            LuaNative.lua_setglobal(L, "Spatial");
        }

        private static void RegisterMethod(IntPtr L, string name, Func<IntPtr, int> fn)
        {
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(fn);
            LuaNative.lua_pushstring(L, name);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_settable(L, -3);
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_QueryPoint(IntPtr L)
        {
            Vector3 point = ReadVector3(L, 1);
            ESpatialObjectType type = (ESpatialObjectType)(byte)LuaNative.lua_tointegerx(L, 4, IntPtr.Zero);
            _results.Clear();
            SpatialRuntime spatial = SpatialRuntime.Instance;
            if (spatial != null)
                spatial.QueryPoint(point, SpatialQueryFilter.ByType(type), _results);
            PushOwners(L, _results);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_QueryRadius(IntPtr L)
        {
            Vector3 center = ReadVector3(L, 1);
            float radius = (float)LuaNative.lua_tonumberx(L, 4, IntPtr.Zero);
            ESpatialObjectType type = (ESpatialObjectType)(byte)LuaNative.lua_tointegerx(L, 5, IntPtr.Zero);
            _results.Clear();
            SpatialRuntime spatial = SpatialRuntime.Instance;
            if (spatial != null)
                spatial.QueryRadius(center, radius, SpatialQueryFilter.ByType(type), _results);
            PushOwners(L, _results);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Contains(IntPtr L)
        {
            Vector3 point = ReadVector3(L, 1);
            ushort ownerId = (ushort)LuaNative.lua_tointegerx(L, 4, IntPtr.Zero);
            ESpatialObjectType type = (ESpatialObjectType)(byte)LuaNative.lua_tointegerx(L, 5, IntPtr.Zero);
            bool contains = SpatialRuntime.Instance != null &&
                            SpatialRuntime.Instance.Contains(point, ownerId, type);
            LuaNative.lua_pushboolean(L, contains ? 1 : 0);
            return 1;
        }

        // ------------------------------------------------------------
        //  Spatial.Raycast(ox,oy,oz, dx,dy,dz, maxDist, type)
        //      -> hit(bool), ownerId, distance, px, py, pz, nx, ny, nz
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Raycast(IntPtr L)
        {
            Vector3 origin = ReadVector3(L, 1);
            Vector3 dir = ReadVector3(L, 4);
            float maxDist = (float)LuaNative.lua_tonumberx(L, 7, IntPtr.Zero);
            ESpatialObjectType type = (ESpatialObjectType)(byte)LuaNative.lua_tointegerx(L, 8, IntPtr.Zero);

            SpatialRuntime spatial = SpatialRuntime.Instance;
            if (spatial == null)
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            bool hit = spatial.Raycast(
                origin,
                dir,
                maxDist,
                SpatialQueryFilter.ByType(type),
                out SpatialHit h);

            if (!hit)
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            LuaNative.lua_pushboolean(L, 1);
            LuaNative.lua_pushinteger(L, h.OwnerId);
            LuaNative.lua_pushnumber(L, h.Distance);
            LuaNative.lua_pushnumber(L, h.Position.x);
            LuaNative.lua_pushnumber(L, h.Position.y);
            LuaNative.lua_pushnumber(L, h.Position.z);
            LuaNative.lua_pushnumber(L, h.Normal.x);
            LuaNative.lua_pushnumber(L, h.Normal.y);
            LuaNative.lua_pushnumber(L, h.Normal.z);
            return 9;
        }

        private static Vector3 ReadVector3(IntPtr L, int index)
        {
            float x = (float)LuaNative.lua_tonumberx(L, index, IntPtr.Zero);
            float y = (float)LuaNative.lua_tonumberx(L, index + 1, IntPtr.Zero);
            float z = (float)LuaNative.lua_tonumberx(L, index + 2, IntPtr.Zero);
            return new Vector3(x, y, z);
        }

        private static void PushOwners(IntPtr L, List<ushort> owners)
        {
            LuaNative.lua_newtable(L);
            for (int i = 0; i < owners.Count; i++)
            {
                LuaNative.lua_pushinteger(L, i + 1);
                LuaNative.lua_pushinteger(L, owners[i]);
                LuaNative.lua_settable(L, -3);
            }
        }
    }
}