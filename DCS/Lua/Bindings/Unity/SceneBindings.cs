using System;
using DCS.Actors;
using DCS.Core;
using DCS.Spatial;
using DCS.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DCS.Lua
{
    public static class SceneBindings
    {
        public static void Register(IntPtr L)
        {
            LuaBindings.RegisterNamespace(L, "Scene", (state, tableIndex) =>
            {
                LuaBindings.RegisterMethod(state, Lua_Load, tableIndex, "Load");
                LuaBindings.RegisterMethod(state, Lua_Unload, tableIndex, "Unload");
                LuaBindings.RegisterMethod(state, Lua_GetCurrent, tableIndex, "GetCurrent");
                LuaBindings.RegisterMethod(state, Lua_IsLoaded, tableIndex, "IsLoaded");
                LuaBindings.RegisterMethod(state, Lua_GetAll, tableIndex, "GetAll");
                LuaBindings.RegisterMethod(state, Lua_GetStatus, tableIndex, "GetStatus");
                LuaBindings.RegisterMethod(state, Lua_RegisterActors, tableIndex, "RegisterActors");
                LuaBindings.RegisterMethod(state, Lua_UnregisterActors, tableIndex, "UnregisterActors");
            });
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Load(IntPtr L)
        {
            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
                return 0;
            string name = LuaArgumentReader.ReadString(L, 2);
            if (string.IsNullOrEmpty(name)) return 0;
            bool withActors = LuaNative.lua_gettop(L) < 3
                || LuaArgumentReader.ReadBool(L, 3);

            DatasetLoader.StreamMapAsync(name, SceneStreamRunner.Instance, dataset =>
            {
                if (withActors) RegisterSceneActors(domain, name);
            });
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Unload(IntPtr L)
        {
            string name = LuaArgumentReader.ReadString(L, 1);
            if (string.IsNullOrEmpty(name)) return 0;

            if (DatasetLoader.CurrentMapName == name)
                DatasetLoader.UnloadCurrent();

            SceneManager.UnloadSceneAsync(name);
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetCurrent(IntPtr L)
        {
            LuaNative.lua_pushstring(L, SceneManager.GetActiveScene().name);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_IsLoaded(IntPtr L)
        {
            string name = LuaArgumentReader.ReadString(L, 1);
            if (string.IsNullOrEmpty(name))
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }
            Scene scene = SceneManager.GetSceneByName(name);
            LuaNative.lua_pushboolean(L, (scene.IsValid() && scene.isLoaded) ? 1 : 0);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetAll(IntPtr L)
        {
            LuaNative.lua_newtable(L);
            int total = SceneManager.sceneCount;
            int idx = 1;
            Scene active = SceneManager.GetActiveScene();
            for (int i = 0; i < total; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;

                LuaNative.lua_newtable(L);

                LuaNative.lua_pushstring(L, "Name");
                LuaNative.lua_pushstring(L, scene.name);
                LuaNative.lua_settable(L, -3);

                LuaNative.lua_pushstring(L, "Handle");
                LuaNative.lua_pushinteger(L, (ushort)Crc32.Get(scene.name));
                LuaNative.lua_settable(L, -3);

                LuaNative.lua_pushstring(L, "IsActive");
                LuaNative.lua_pushboolean(L, scene == active ? 1 : 0);
                LuaNative.lua_settable(L, -3);

                LuaNative.lua_rawseti(L, -2, idx++);
            }
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetStatus(IntPtr L)
        {
            string state = DatasetLoader.IsLoading
                ? "Loading"
                : (!string.IsNullOrEmpty(DatasetLoader.CurrentMapName) ? "Loaded" : "Unloaded");

            LuaNative.lua_pushstring(L, state);
            LuaNative.lua_pushstring(L, DatasetLoader.CurrentMapName ?? string.Empty);
            LuaNative.lua_pushnumber(L, Math.Round(DatasetLoader.LoadingProgress * 100.0, 1));
            return 3;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_RegisterActors(IntPtr L)
        {
            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
            {
                LuaNative.lua_pushinteger(L, 0);
                return 1;
            }
            string name = LuaArgumentReader.ReadString(L, 2);
            int count = RegisterSceneActors(domain, name);
            LuaNative.lua_pushinteger(L, count);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_UnregisterActors(IntPtr L)
        {
            return 0;
        }

        private static int RegisterSceneActors(Domain domain, string sceneName)
        {
            Scene scene = string.IsNullOrEmpty(sceneName)
                ? SceneManager.GetActiveScene()
                : SceneManager.GetSceneByName(sceneName);

            if (!scene.IsValid() || !scene.isLoaded)
                return 0;

            BaseActor[] actors = UnityEngine.Object.FindObjectsByType<BaseActor>();
            int registered = 0;

            for (int i = 0; i < actors.Length; i++)
            {
                BaseActor actor = actors[i];
                if (actor == null) continue;
                if (actor.gameObject.scene != scene) continue;

                Host host = actor.Host;
                if (!HostManager.IsValid(host)) continue;

                // PositionComponent — через обычный ComponentPool.
                Handle hPos = DCSystem.Allocate<PositionComponent>(host, domain.HostChain);
                ref var pos = ref DCSystem.ResolveHandle<PositionComponent>(hPos);
                pos.Position = actor.transform.position;

                // NameComponent — через обычный ComponentPool.
                string actorName = string.IsNullOrEmpty(actor.SearchName)
                    ? actor.name
                    : actor.SearchName;
                if (!string.IsNullOrEmpty(actorName))
                {
                    Handle hName = DCSystem.Allocate<NameComponent>(host, domain.HostChain);
                    ref var nameComp = ref DCSystem.ResolveHandle<NameComponent>(hName);
                    nameComp.Name = actorName;
                }

                registered++;
            }

            return registered;
        }
    }
}