using DCS.Spatial;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DCS.Lua.Bindings
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

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_QueryPoint(IntPtr L)
        {
            Vector3 point = ReadVector3(L, 1);
            ESpatialObjectType type = (ESpatialObjectType)(byte)LuaArgumentReader.ReadInt(L, 4);

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
            float radius = LuaArgumentReader.ReadFloat(L, 4);
            ESpatialObjectType type = (ESpatialObjectType)(byte)LuaArgumentReader.ReadInt(L, 5);

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
            ushort ownerId = (ushort)LuaArgumentReader.ReadInt(L, 4);
            ESpatialObjectType type = (ESpatialObjectType)(byte)LuaArgumentReader.ReadInt(L, 5);

            bool contains = SpatialRuntime.Instance != null &&
                            SpatialRuntime.Instance.Contains(point, ownerId, type);

            LuaNative.lua_pushboolean(L, contains ? 1 : 0);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Raycast(IntPtr L)
        {
            Vector3 origin = ReadVector3(L, 1);
            Vector3 dir = ReadVector3(L, 4);
            float maxDist = LuaArgumentReader.ReadFloat(L, 7);
            ESpatialObjectType type = (ESpatialObjectType)(byte)LuaArgumentReader.ReadInt(L, 8);

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