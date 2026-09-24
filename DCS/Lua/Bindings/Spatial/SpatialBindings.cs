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
        private const int MaxObjectType = (int)ESpatialObjectType.NavigationSurface;

        public static void Register(IntPtr L)
        {
            LuaNative.lua_newtable(L);
            LuaBindings.RegisterMethod(L, Lua_QueryRadius, "QueryRadius");
            LuaBindings.RegisterMethod(L, Lua_QueryPoint, "QueryPoint");
            LuaBindings.RegisterMethod(L, Lua_Contains, "Contains");
            LuaBindings.RegisterMethod(L, Lua_Raycast, "RaycastNative");
            LuaNative.lua_setglobal(L, "Spatial");
        }

        // ------------------------------------------------------------
        //  Spatial.QueryPoint(x, y, z, type) -> { ownerId, ... }
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_QueryPoint(IntPtr L)
        {
            if (!TryReadVector3(L, 1, "QueryPoint", out Vector3 point))
                return LuaNative.lua_error(L,
                    "[SpatialBindings] QueryPoint: expected numbers at args 1..3");

            if (!TryReadObjectType(L, 4, "QueryPoint", out ESpatialObjectType type))
                return LuaNative.lua_error(L,
                    $"[SpatialBindings] QueryPoint: type out of range [0, {MaxObjectType}]");

            SpatialRuntime spatial = SpatialRuntime.Instance;
            if (spatial == null)
                return LuaNative.lua_error(L,
                    "[SpatialBindings] QueryPoint: SpatialRuntime not initialized");

            var results = new List<ushort>(64);
            spatial.QueryPoint(point, SpatialQueryFilter.ByType(type), results);
            PushOwners(L, results);
            return 1;
        }

        // ------------------------------------------------------------
        //  Spatial.QueryRadius(x, y, z, radius, type) -> { ownerId, ... }
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_QueryRadius(IntPtr L)
        {
            if (!TryReadVector3(L, 1, "QueryRadius", out Vector3 center))
                return LuaNative.lua_error(L,
                    "[SpatialBindings] QueryRadius: expected numbers at args 1..3");

            float radius = (float)LuaNative.lua_tonumberx(L, 4, IntPtr.Zero);
            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius < 0f)
                return LuaNative.lua_error(L,
                    $"[SpatialBindings] QueryRadius: invalid radius {radius}");

            if (!TryReadObjectType(L, 5, "QueryRadius", out ESpatialObjectType type))
                return LuaNative.lua_error(L,
                    $"[SpatialBindings] QueryRadius: type out of range [0, {MaxObjectType}]");

            SpatialRuntime spatial = SpatialRuntime.Instance;
            if (spatial == null)
                return LuaNative.lua_error(L,
                    "[SpatialBindings] QueryRadius: SpatialRuntime not initialized");

            var results = new List<ushort>(64);
            spatial.QueryRadius(center, radius, SpatialQueryFilter.ByType(type), results);
            PushOwners(L, results);
            return 1;
        }

        // ------------------------------------------------------------
        //  Spatial.Contains(x, y, z, ownerId, type) -> bool
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Contains(IntPtr L)
        {
            if (!TryReadVector3(L, 1, "Contains", out Vector3 point))
                return LuaNative.lua_error(L,
                    "[SpatialBindings] Contains: expected numbers at args 1..3");

            long ownerRaw = LuaNative.lua_tointegerx(L, 4, IntPtr.Zero);
            if (ownerRaw < 0 || ownerRaw > ushort.MaxValue)
                return LuaNative.lua_error(L,
                    $"[SpatialBindings] Contains: ownerId {ownerRaw} out of range [0, {ushort.MaxValue}]");
            ushort ownerId = (ushort)ownerRaw;

            if (!TryReadObjectType(L, 5, "Contains", out ESpatialObjectType type))
                return LuaNative.lua_error(L,
                    $"[SpatialBindings] Contains: type out of range [0, {MaxObjectType}]");

            SpatialRuntime spatial = SpatialRuntime.Instance;
            if (spatial == null)
                return LuaNative.lua_error(L,
                    "[SpatialBindings] Contains: SpatialRuntime not initialized");

            bool contains = spatial.Contains(point, ownerId, type);
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
            if (!TryReadVector3(L, 1, "Raycast", out Vector3 origin))
                return LuaNative.lua_error(L,
                    "[SpatialBindings] Raycast: expected numbers at args 1..3 (origin)");

            if (!TryReadVector3(L, 4, "Raycast", out Vector3 dir))
                return LuaNative.lua_error(L,
                    "[SpatialBindings] Raycast: expected numbers at args 4..6 (direction)");

            if (dir.sqrMagnitude < 1e-8f)
                return LuaNative.lua_error(L,
                    "[SpatialBindings] Raycast: direction is zero-length");

            float maxDist = (float)LuaNative.lua_tonumberx(L, 7, IntPtr.Zero);
            if (float.IsNaN(maxDist) || float.IsInfinity(maxDist) || maxDist < 0f)
                return LuaNative.lua_error(L,
                    $"[SpatialBindings] Raycast: invalid maxDist {maxDist}");

            if (!TryReadObjectType(L, 8, "Raycast", out ESpatialObjectType type))
                return LuaNative.lua_error(L,
                    $"[SpatialBindings] Raycast: type out of range [0, {MaxObjectType}]");

            SpatialRuntime spatial = SpatialRuntime.Instance;
            if (spatial == null)
                return LuaNative.lua_error(L,
                    "[SpatialBindings] Raycast: SpatialRuntime not initialized");

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

        // ------------------------------------------------------------
        //  Хелперы
        // ------------------------------------------------------------
        private static bool TryReadVector3(IntPtr L, int index, string method, out Vector3 v)
        {
            v = default;
            for (int i = 0; i < 3; i++)
            {
                if (LuaNative.lua_type(L, index + i) != LuaNative.LUA_TNUMBER)
                    return false;
            }
            v = new Vector3(
                (float)LuaNative.lua_tonumberx(L, index, IntPtr.Zero),
                (float)LuaNative.lua_tonumberx(L, index + 1, IntPtr.Zero),
                (float)LuaNative.lua_tonumberx(L, index + 2, IntPtr.Zero));
            return true;
        }

        private static bool TryReadObjectType(IntPtr L, int index, string method, out ESpatialObjectType type)
        {
            type = default;
            if (LuaNative.lua_type(L, index) != LuaNative.LUA_TNUMBER)
                return false;
            long raw = LuaNative.lua_tointegerx(L, index, IntPtr.Zero);
            if (raw < 0 || raw > MaxObjectType)
                return false;
            type = (ESpatialObjectType)raw;
            return true;
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