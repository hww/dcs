// === FILE: Lua/Bindings/GameplayLuaBindings.cs ===
// High-performance native bindings for Encounter, StrongPoint, and Spatial queries.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DCS.Core;
using DCS.Gameplay;
using DCS.Spatial;
using UnityEngine;

namespace DCS.Lua.Bindings
{
    public static class GameplayLuaBindings
    {
        // Reusable static buffer to prevent runtime GC allocations during high-frequency spatial queries
        private static readonly List<ushort> _queryResultBuffer = new List<ushort>(256);

        public static void Register(IntPtr L)
        {
            // --- ENCOUNTER API ---
            LuaNative.lua_newtable(L);
            RegisterMethod(L, "Activate", Lua_ActivateEncounter);
            RegisterMethod(L, "Deactivate", Lua_DeactivateEncounter);
            RegisterMethod(L, "Complete", Lua_CompleteEncounter);
            RegisterMethod(L, "Fail", Lua_FailEncounter);
            LuaNative.lua_setglobal(L, "Encounter");

            // --- SPATIAL QUERIES API ---
            LuaNative.lua_newtable(L);
            RegisterMethod(L, "QueryRadius", Lua_SpatialQueryRadius);
            RegisterMethod(L, "Raycast", Lua_SpatialRaycast);
            RegisterMethod(L, "Contains", Lua_SpatialContains);
            LuaNative.lua_setglobal(L, "Spatial");

            // --- AI & COGNITIVE ROLES API ---
            LuaNative.lua_newtable(L);
            RegisterMethod(L, "SetCombatRole", Lua_SetCombatRole);
            LuaNative.lua_setglobal(L, "AI");
        }

        private static void RegisterMethod(IntPtr L, string name, Func<IntPtr, int> fn)
        {
            LuaNative.lua_pushstring(L, name);
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(fn);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_settable(L, -3);
        }

        // ============================================================
        //  ENCOUNTER SYSTEM BINDINGS
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_ActivateEncounter(IntPtr L)
        {
            ushort id = (ushort)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            if (GameplayRuntime.Instance != null && GameplayRuntime.Instance.Encounters.TryGet(id, out var runtime))
            {
                LuaNative.lua_pushboolean(L, runtime.Activate());
                return 1;
            }
            LuaNative.lua_pushboolean(L, false);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_DeactivateEncounter(IntPtr L)
        {
            ushort id = (ushort)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            if (GameplayRuntime.Instance != null && GameplayRuntime.Instance.Encounters.TryGet(id, out var runtime))
            {
                LuaNative.lua_pushboolean(L, runtime.Deactivate());
                return 1;
            }
            LuaNative.lua_pushboolean(L, false);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_CompleteEncounter(IntPtr L)
        {
            ushort id = (ushort)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            if (GameplayRuntime.Instance != null && GameplayRuntime.Instance.Encounters.TryGet(id, out var runtime))
            {
                LuaNative.lua_pushboolean(L, runtime.Complete());
                return 1;
            }
            LuaNative.lua_pushboolean(L, false);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_FailEncounter(IntPtr L)
        {
            ushort id = (ushort)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            if (GameplayRuntime.Instance != null && GameplayRuntime.Instance.Encounters.TryGet(id, out var runtime))
            {
                LuaNative.lua_pushboolean(L, runtime.Fail());
                return 1;
            }
            LuaNative.lua_pushboolean(L, false);
            return 1;
        }

        // ============================================================
        //  SPATIAL INDEX SYSTEM BINDINGS (Zero GC)
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_SpatialQueryRadius(IntPtr L)
        {
            float x = (float)LuaNative.lua_tonumberx(L, 1, IntPtr.Zero);
            float y = (float)LuaNative.lua_tonumberx(L, 2, IntPtr.Zero);
            float z = (float)LuaNative.lua_tonumberx(L, 3, IntPtr.Zero);
            float radius = (float)LuaNative.lua_tonumberx(L, 4, IntPtr.Zero);
            byte objectTypeByte = (byte)LuaNative.lua_tointegerx(L, 5, IntPtr.Zero);

            Vector3 center = new Vector3(x, y, z);
            _queryResultBuffer.Clear();

            var filter = new SpatialQueryFilter
            {
                FilterByType = objectTypeByte != 0,
                ObjectType = (ESpatialObjectType)objectTypeByte
            };

            if (SpatialRuntime.Instance != null)
            {
                SpatialRuntime.Instance.QueryRadius(center, radius, filter, _queryResultBuffer);
            }

            // Pack results into a lightweight, sequential Lua table array
            LuaNative.lua_createtable(L, _queryResultBuffer.Count, 0);
            for (int i = 0; i < _queryResultBuffer.Count; i++)
            {
                LuaNative.lua_pushinteger(L, i + 1);
                LuaNative.lua_pushinteger(L, _queryResultBuffer[i]);
                LuaNative.lua_settable(L, -3);
            }

            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_SpatialRaycast(IntPtr L)
        {
            float ox = (float)LuaNative.lua_tonumberx(L, 1, IntPtr.Zero);
            float oy = (float)LuaNative.lua_tonumberx(L, 2, IntPtr.Zero);
            float oz = (float)LuaNative.lua_tonumberx(L, 3, IntPtr.Zero);

            float dx = (float)LuaNative.lua_tonumberx(L, 4, IntPtr.Zero);
            float dy = (float)LuaNative.lua_tonumberx(L, 5, IntPtr.Zero);
            float dz = (float)LuaNative.lua_tonumberx(L, 6, IntPtr.Zero);

            float maxDistance = (float)LuaNative.lua_tonumberx(L, 7, IntPtr.Zero);
            byte typeFilter = (byte)LuaNative.lua_tointegerx(L, 8, IntPtr.Zero);

            Vector3 origin = new Vector3(ox, oy, oz);
            Vector3 direction = new Vector3(dx, dy, dz);

            var filter = new SpatialQueryFilter
            {
                FilterByType = typeFilter != 0,
                ObjectType = (ESpatialObjectType)typeFilter
            };

            if (SpatialRuntime.Instance != null && SpatialRuntime.Instance.Raycast(origin, direction, maxDistance, filter, out SpatialHit hit))
            {
                LuaNative.lua_pushboolean(L, true);
                LuaNative.lua_pushinteger(L, hit.OwnerId);
                LuaNative.lua_pushnumber(L, hit.Distance);
                // Return hit position coordinates sequentially
                LuaNative.lua_pushnumber(L, hit.Position.x);
                LuaNative.lua_pushnumber(L, hit.Position.y);
                LuaNative.lua_pushnumber(L, hit.Position.z);
                return 6;
            }

            LuaNative.lua_pushboolean(L, false);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_SpatialContains(IntPtr L)
        {
            float x = (float)LuaNative.lua_tonumberx(L, 1, IntPtr.Zero);
            float y = (float)LuaNative.lua_tonumberx(L, 2, IntPtr.Zero);
            float z = (float)LuaNative.lua_tonumberx(L, 3, IntPtr.Zero);
            ushort ownerId = (ushort)LuaNative.lua_tointegerx(L, 4, IntPtr.Zero);
            byte type = (byte)LuaNative.lua_tointegerx(L, 5, IntPtr.Zero);

            Vector3 point = new Vector3(x, y, z);
            bool inside = SpatialRuntime.Instance != null && SpatialRuntime.Instance.Contains(point, ownerId, (ESpatialObjectType)type);

            LuaNative.lua_pushboolean(L, inside);
            return 1;
        }

        // ============================================================
        //  AI / COGNITIVE SYSTEMS BINDINGS
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_SetCombatRole(IntPtr L)
        {
            int packedHost = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            byte roleByte = (byte)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            ushort strongPointId = (ushort)LuaNative.lua_tointegerx(L, 3, IntPtr.Zero);

            Handle hostHandle = new Handle(packedHost);
            if (hostHandle.Id >= HostManager.GlobalHosts.Length) return 0;

            ref var hostData = ref HostManager.GlobalHosts[hostHandle.Id];
            if (hostData.Generation != hostHandle.Generation) return 0;

            Host host = new Host(hostHandle.Id, hostHandle.Generation);

            // Allocate or extract Role allocation structures seamlessly 
            // Note: Assuming CombatRoleComponent is mapped to its type inside ComponentRegistry
            int typeId = 4; // Use your actual generated ComponentType ID or resolve dynamically
            var pool = ComponentRegistry.Pools[typeId];

            if (pool != null && LuaManager._globalHostChain != null)
            {
                var node = LuaManager._globalHostChain.GetTypedHandle(host, typeId);
                Handle cHandle = node.IsNull ? pool.SystemAllocate(host, LuaManager._globalHostChain) : node.Component;

                // Directly write layout properties inside C# memory space using our zero GC JIT architecture
                pool.SetField(cHandle.Id, "CurrentRole", L);
                // assuming Stack top contains role or pass manually
            }
            return 0;
        }
    }
}