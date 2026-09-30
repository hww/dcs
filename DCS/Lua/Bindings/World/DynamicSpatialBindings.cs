using DCS.Core;
using DCS.Spatial;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace DCS.Lua
{
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

        // DynamicSpatial.QueryRadius(domainId, x, y, z, radius [, type]) -> array
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_QueryRadius(IntPtr L)
        {
            var args = new ArgReader(L, "DynamicSpatial.QueryRadius");
            args.ExpectInRange(5, 6);

            args.CheckUserdataRaw(1);
            Vector3 center = ReadVector3(args, 2);
            float radius = (float)args.CheckNumber(5);

            ESpatialObjectType type = ESpatialObjectType.Generic;
            if (args.Count >= 6)
                type = (ESpatialObjectType)(byte)args.CheckInteger(6);

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain) ||
                !domain.HasSpatial ||
                !SpatialDomainRegistry.TryGet(domain.SpatialDomainId, out SpatialDomain spatial))
            {
                LuaNative.lua_newtable(L);
                return 1;
            }

            _results.Clear();
            var filter = type == ESpatialObjectType.Generic
                ? SpatialQueryFilter.Any
                : SpatialQueryFilter.ByType(type);
            spatial.DynamicSpatial.QueryRadius(center, radius, filter, _results);
            PushHostArray(L, _results);
            return 1;
        }

        // DynamicSpatial.QueryPoint(domainId, x, y, z [, type]) -> array
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_QueryPoint(IntPtr L)
        {
            var args = new ArgReader(L, "DynamicSpatial.QueryPoint");
            args.ExpectInRange(4, 5);

            args.CheckUserdataRaw(1);
            Vector3 point = ReadVector3(args, 2);

            ESpatialObjectType type = ESpatialObjectType.Generic;
            if (args.Count >= 5)
                type = (ESpatialObjectType)(byte)args.CheckInteger(5);

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain) ||
                !domain.HasSpatial ||
                !SpatialDomainRegistry.TryGet(domain.SpatialDomainId, out SpatialDomain spatial))
            {
                LuaNative.lua_newtable(L);
                return 1;
            }

            _results.Clear();
            var filter = type == ESpatialObjectType.Generic
                ? SpatialQueryFilter.Any
                : SpatialQueryFilter.ByType(type);
            // Approximate point query with tiny radius. Replace with real
            // QueryPoint on DynamicSpatial if you add one.
            spatial.DynamicSpatial.QueryRadius(point, 0.001f, filter, _results);
            PushHostArray(L, _results);
            return 1;
        }

        // DynamicSpatial.Contains(domainId, x, y, z, hostId) -> bool
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Contains(IntPtr L)
        {
            var args = new ArgReader(L, "DynamicSpatial.Contains");
            args.ExpectExactly(5);

            args.CheckUserdataRaw(1);
            Vector3 point = ReadVector3(args, 2);
            int hostId = (int)args.CheckInteger(5);

            // Not implemented yet. Return false.
            // Когда реализуешь — используй point и hostId.
            _ = point;
            _ = hostId;
            LuaNative.lua_pushboolean(L, 0);
            return 1;
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