using DCS.Spatial;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace DCS.Lua
{
    public static class SpatialBindings
    {
        private static readonly List<ushort> _results = new List<ushort>(64);

        public static void Register(IntPtr L)
        {
            LuaBindings.RegisterNamespace(L, "Spatial", (state, tableIndex) =>
            {
                LuaBindings.RegisterMethod(state, Lua_QueryRadius, tableIndex, "QueryRadius");
                LuaBindings.RegisterMethod(state, Lua_QueryPoint, tableIndex, "QueryPoint");
                LuaBindings.RegisterMethod(state, Lua_Contains, tableIndex, "Contains");
                LuaBindings.RegisterMethod(state, Lua_Raycast, tableIndex, "Raycast");
            });
        }

        // ============================================================
        //  QueryPoint(domain, x, y, z, type) -> table
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_QueryPoint(IntPtr L)
        {
            var args = new ArgReader(L, "Spatial.QueryPoint");
            args.ExpectExactly(5);

            args.CheckUserdataRaw(1);
            Vector3 point = ReadVector3(args, 2);
            ESpatialObjectType type = (ESpatialObjectType)(byte)args.CheckInteger(5);

            if (!HostResolver.TryGetSpatialDomain(L, 1, out var sd))
            {
                LuaNative.lua_newtable(L);
                return 1;
            }

            _results.Clear();
            var spatial = sd.StaticSpatial;
            if (spatial != null)
                spatial.QueryPoint(point, SpatialQueryFilter.ByType(type), _results);

            PushOwners(L, _results);
            return 1;
        }

        // ============================================================
        //  QueryRadius(domain, x, y, z, radius, type) -> table
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_QueryRadius(IntPtr L)
        {
            var args = new ArgReader(L, "Spatial.QueryRadius");
            args.ExpectExactly(6);

            args.CheckUserdataRaw(1);
            Vector3 center = ReadVector3(args, 2);
            float radius = (float)args.CheckNumber(5);
            ESpatialObjectType type = (ESpatialObjectType)(byte)args.CheckInteger(6);

            if (!HostResolver.TryGetSpatialDomain(L, 1, out var sd))
            {
                LuaNative.lua_newtable(L);
                return 1;
            }

            _results.Clear();
            var spatial = sd.StaticSpatial;
            if (spatial != null)
                spatial.QueryRadius(center, radius, SpatialQueryFilter.ByType(type), _results);

            PushOwners(L, _results);
            return 1;
        }

        // ============================================================
        //  Contains(domain, x, y, z, ownerId, type) -> bool
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Contains(IntPtr L)
        {
            var args = new ArgReader(L, "Spatial.Contains");
            args.ExpectExactly(6);

            args.CheckUserdataRaw(1);
            Vector3 point = ReadVector3(args, 2);
            ushort ownerId = (ushort)args.CheckInteger(5);
            ESpatialObjectType type = (ESpatialObjectType)(byte)args.CheckInteger(6);

            if (!HostResolver.TryGetSpatialDomain(L, 1, out var sd))
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            var spatial = sd.StaticSpatial;
            bool contains = spatial.Contains(point, ownerId, type);

            LuaNative.lua_pushboolean(L, contains ? 1 : 0);
            return 1;
        }

        // ============================================================
        //  Raycast(domain, ox, oy, oz, dx, dy, dz, maxDist, type)
        //    -> bool, ownerId, dist, px, py, pz, nx, ny, nz
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Raycast(IntPtr L)
        {
            var args = new ArgReader(L, "Spatial.Raycast");
            args.ExpectExactly(9);

            args.CheckUserdataRaw(1);
            Vector3 origin = ReadVector3(args, 2);
            Vector3 dir = ReadVector3(args, 5);
            float maxDist = (float)args.CheckNumber(8);
            ESpatialObjectType type = (ESpatialObjectType)(byte)args.CheckInteger(9);

            if (!HostResolver.TryGetSpatialDomain(L, 1, out var sd))
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            var spatial = sd.StaticSpatial;
            if (spatial == null)
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            bool hit = spatial.Raycast(
                origin, dir, maxDist,
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

        // ============================================================
        //  Внутреннее
        // ============================================================

        private static Vector3 ReadVector3(ArgReader args, int startIndex)
        {
            float x = (float)args.CheckNumber(startIndex);
            float y = (float)args.CheckNumber(startIndex + 1);
            float z = (float)args.CheckNumber(startIndex + 2);
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