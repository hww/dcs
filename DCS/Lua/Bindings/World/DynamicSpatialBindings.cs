using DCS.Core;
using DCS.Gameplay;
using DCS.Spatial;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DCS.Lua.Bindings
{
    /// <summary>
    /// Queries over DynamicSpatial (runtime, moving hosts).
    /// Returns Host packed ints.
    /// Lua signature: center is 3 numbers, then radius, then optional type.
    /// </summary>
    public static class DynamicSpatialBindings
    {
        private static readonly List<ushort> _results = new List<ushort>(64);

        public static void Register(IntPtr L)
        {
            LuaBindings.RegisterNamespace(L, "DynamicSpatial", (state, tableIndex) =>
            {
                LuaBindings.RegisterMethod(state, Lua_QueryRadius, tableIndex, "QueryRadius");
                LuaBindings.RegisterMethod(state, Lua_QueryPoint, tableIndex, "QueryPoint");
                LuaBindings.RegisterMethod(state, Lua_Contains, tableIndex, "Contains");
            });
        }

        // ------------------------------------------------------------
        // DynamicSpatial.QueryRadius(x, y, z, radius [, type]) -> array
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_QueryRadius(IntPtr L)
        {
            Vector3 center = ReadVector3(L, 1);
            float radius = LuaArgumentReader.ReadFloat(L, 4);
            ESpatialObjectType type = ReadTypeOrDefault(L, 5);

            var spatial = SpatialBootstrap.Instance != null
                ? SpatialBootstrap.Instance.Spatial
                : null;
            if (spatial == null)
            {
                LuaNative.lua_newtable(L);
                return 1;
            }

            _results.Clear();
            var filter = type == ESpatialObjectType.Generic
                ? SpatialQueryFilter.Any
                : SpatialQueryFilter.ByType(type);
            spatial.QueryRadius(center, radius, filter, _results);
            PushHostArray(L, _results);
            return 1;
        }

        // ------------------------------------------------------------
        // DynamicSpatial.QueryPoint(x, y, z [, type]) -> array
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_QueryPoint(IntPtr L)
        {
            // Note: DynamicSpatial currently has only QueryRadius.
            // For a true point query, use a small radius or implement
            // QueryPoint on DynamicSpatial. Here we approximate with radius 0.
            Vector3 point = ReadVector3(L, 1);
            ESpatialObjectType type = ReadTypeOrDefault(L, 4);

            var spatial = SpatialBootstrap.Instance != null
                ? SpatialBootstrap.Instance.Spatial
                : null;
            if (spatial == null)
            {
                LuaNative.lua_newtable(L);
                return 1;
            }

            _results.Clear();
            var filter = type == ESpatialObjectType.Generic
                ? SpatialQueryFilter.Any
                : SpatialQueryFilter.ByType(type);
            spatial.QueryRadius(point, 0.001f, filter, _results);
            PushHostArray(L, _results);
            return 1;
        }

        // ------------------------------------------------------------
        // DynamicSpatial.Contains(x, y, z, hostId) -> bool
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Contains(IntPtr L)
        {
            // Placeholder: DynamicSpatial doesn't track per-host containment
            // the same way SpatialRuntime does. Return false for now,
            // or extend DynamicSpatial with a Contains(hostId, point) method.
            LuaNative.lua_pushboolean(L, 0);
            return 1;
        }

        private static Vector3 ReadVector3(IntPtr L, int index)
        {
            float x = LuaArgumentReader.ReadFloat(L, index);
            float y = LuaArgumentReader.ReadFloat(L, index + 1);
            float z = LuaArgumentReader.ReadFloat(L, index + 2);
            return new Vector3(x, y, z);
        }

        private static ESpatialObjectType ReadTypeOrDefault(IntPtr L, int index)
        {
            if (LuaNative.lua_gettop(L) < index) return ESpatialObjectType.Generic;
            int t = LuaArgumentReader.ReadInt(L, index);
            if (t < 0 || t > byte.MaxValue) return ESpatialObjectType.Generic;
            return (ESpatialObjectType)(byte)t;
        }

        private static void PushHostArray(IntPtr L, List<ushort> hosts)
        {
            LuaNative.lua_newtable(L);
            for (int i = 0; i < hosts.Count; i++)
            {
                ushort id = hosts[i];
                if (id >= HostManager.MaxGameObjects) continue;
                Host h = new Host
                {
                    Id = id,
                    Generation = HostManager.GlobalHosts[id].Generation
                };
                LuaNative.lua_pushinteger(L, i + 1);
                LuaNative.lua_pushinteger(L, h.ToLua());
                LuaNative.lua_settable(L, -3);
            }
        }
    }
}