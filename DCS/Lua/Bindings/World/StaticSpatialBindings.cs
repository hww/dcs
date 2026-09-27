using DCS.Core;
using DCS.Spatial;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DCS.Lua
{
    /// <summary>
    /// Запросы к запечённому (статическому) SpatialRuntime из Lua.
    /// Первый аргумент каждой функции — domainId.
    /// </summary>
    public static class StaticSpatialBindings
    {
        private static readonly List<ushort> _results = new List<ushort>(64);

        public static void Register(IntPtr L)
        {
            LuaBindings.RegisterNamespace(L, "StaticSpatial", (state, tableIndex) =>
            {
                LuaBindings.RegisterMethod(state, Lua_QueryRadius, tableIndex, "QueryRadius");
                LuaBindings.RegisterMethod(state, Lua_QueryPoint, tableIndex, "QueryPoint");
                LuaBindings.RegisterMethod(state, Lua_Contains, tableIndex, "Contains");
                LuaBindings.RegisterMethod(state, Lua_Raycast, tableIndex, "Raycast");
            });
        }

        // StaticSpatial.QueryPoint(domainId, x, y, z, type) -> array
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_QueryPoint(IntPtr L)
        {
            if (!TryGetSpatial(L, out var spatial))
            {
                LuaNative.lua_newtable(L);
                return 1;
            }

            Vector3 point = ReadVector3(L, 2);
            ESpatialObjectType type = (ESpatialObjectType)(byte)LuaArgumentReader.ReadInt(L, 5);

            _results.Clear();
            spatial.QueryPoint(point, SpatialQueryFilter.ByType(type), _results);
            PushOwners(L, _results);
            return 1;
        }

        // StaticSpatial.QueryRadius(domainId, x, y, z, radius, type) -> array
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_QueryRadius(IntPtr L)
        {
            if (!TryGetSpatial(L, out var spatial))
            {
                LuaNative.lua_newtable(L);
                return 1;
            }

            Vector3 center = ReadVector3(L, 2);
            float radius = LuaArgumentReader.ReadFloat(L, 5);
            ESpatialObjectType type = (ESpatialObjectType)(byte)LuaArgumentReader.ReadInt(L, 6);

            _results.Clear();
            spatial.QueryRadius(center, radius, SpatialQueryFilter.ByType(type), _results);
            PushOwners(L, _results);
            return 1;
        }

        // StaticSpatial.Contains(domainId, x, y, z, ownerId, type) -> bool
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Contains(IntPtr L)
        {
            if (!TryGetSpatial(L, out var spatial))
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            Vector3 point = ReadVector3(L, 2);
            ushort ownerId = (ushort)LuaArgumentReader.ReadInt(L, 5);
            ESpatialObjectType type = (ESpatialObjectType)(byte)LuaArgumentReader.ReadInt(L, 6);

            bool contains = spatial.Contains(point, ownerId, type);
            LuaNative.lua_pushboolean(L, contains ? 1 : 0);
            return 1;
        }

        // StaticSpatial.Raycast(domainId, ox,oy,oz, dx,dy,dz, maxDist, type) -> false | 9 values
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Raycast(IntPtr L)
        {
            if (!TryGetSpatial(L, out var spatial))
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            Vector3 origin = ReadVector3(L, 2);
            Vector3 dir = ReadVector3(L, 5);
            float maxDist = LuaArgumentReader.ReadFloat(L, 8);
            ESpatialObjectType type = (ESpatialObjectType)(byte)LuaArgumentReader.ReadInt(L, 9);

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

        /// <summary>
        /// Резолвит SpatialRuntime через первый Lua-аргумент (domainId).
        /// </summary>
        private static bool TryGetSpatial(IntPtr L, out SpatialRuntime spatial)
        {
            spatial = null;
            if (!HostResolver.TryGetDomain(L, 1, out Domain domain)) return false;
            if (!domain.HasSpatial) return false;
            if (!SpatialDomainRegistry.TryGet(domain.SpatialDomainId, out var sd)) return false;
            spatial = sd.StaticSpatial;
            return spatial != null;
        }

        private static Vector3 ReadVector3(IntPtr L, int index)
        {
            float x = LuaArgumentReader.ReadFloat(L, index);
            float y = LuaArgumentReader.ReadFloat(L, index + 1);
            float z = LuaArgumentReader.ReadFloat(L, index + 2);
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