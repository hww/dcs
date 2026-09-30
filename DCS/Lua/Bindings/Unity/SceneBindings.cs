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

        // ============================================================
        //  Load(domain, name [, withActors])
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Load(IntPtr L)
        {
            var args = new ArgReader(L, "Scene.Load");
            args.ExpectInRange(2, 3);

            args.CheckInteger(1);
            string name = args.CheckString(2);
            if (string.IsNullOrEmpty(name))
                LuaFail.Fail(L, "Scene.Load", "argument #2: scene name must not be empty");

            bool withActors = true;
            if (args.Count >= 3)
            {
                if (!args.TryBool(3, out bool v))
                    withActors = true;      // аргумента нет — дефолт
                else if (v == false && !IsExplicitNil(L, 3))
                    withActors = false;
                // если явный nil — оставляем true (дефолт)
                else
                    withActors = v;
            }

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
                return 0;

            // managed-работа — после всех проверок
            DatasetLoader.StreamMapAsync(name, SceneStreamRunner.Instance, dataset =>
            {
                if (withActors) RegisterSceneActors(L, domain, name);
            });

            return 0;
        }

        // ============================================================
        //  Unload(name)
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Unload(IntPtr L)
        {
            var args = new ArgReader(L, "Scene.Unload");
            args.ExpectExactly(1);

            string name = args.CheckString(1);
            if (string.IsNullOrEmpty(name))
                LuaFail.Fail(L, "Scene.Unload", "argument #1: scene name must not be empty");

            if (DatasetLoader.CurrentMapName == name)
                DatasetLoader.UnloadCurrent();

            SceneManager.UnloadSceneAsync(name);
            return 0;
        }

        // ============================================================
        //  GetCurrent() -> name
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetCurrent(IntPtr L)
        {
            var args = new ArgReader(L, "Scene.GetCurrent");
            args.ExpectExactly(0);

            LuaNative.lua_pushstring(L, SceneManager.GetActiveScene().name);
            return 1;
        }

        // ============================================================
        //  IsLoaded(name) -> bool
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_IsLoaded(IntPtr L)
        {
            var args = new ArgReader(L, "Scene.IsLoaded");
            args.ExpectExactly(1);

            string name = args.CheckString(1);
            if (string.IsNullOrEmpty(name))
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            Scene scene = SceneManager.GetSceneByName(name);
            LuaNative.lua_pushboolean(L, (scene.IsValid() && scene.isLoaded) ? 1 : 0);
            return 1;
        }

        // ============================================================
        //  GetAll() -> table
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetAll(IntPtr L)
        {
            var args = new ArgReader(L, "Scene.GetAll");
            args.ExpectExactly(0);

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

        // ============================================================
        //  GetStatus() -> state, name, percent
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetStatus(IntPtr L)
        {
            var args = new ArgReader(L, "Scene.GetStatus");
            args.ExpectExactly(0);

            string state = DatasetLoader.IsLoading
                ? "Loading"
                : (!string.IsNullOrEmpty(DatasetLoader.CurrentMapName) ? "Loaded" : "Unloaded");

            LuaNative.lua_pushstring(L, state);
            LuaNative.lua_pushstring(L, DatasetLoader.CurrentMapName ?? string.Empty);
            LuaNative.lua_pushnumber(L, Math.Round(DatasetLoader.LoadingProgress * 100.0, 1));
            return 3;
        }

        // ============================================================
        //  RegisterActors(domain, name) -> count
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_RegisterActors(IntPtr L)
        {
            var args = new ArgReader(L, "Scene.RegisterActors");
            args.ExpectExactly(2);

            args.CheckInteger(1);
            string name = args.CheckString(2);

            if (!HostResolver.TryGetDomain(L, 1, out Domain domain))
            {
                LuaNative.lua_pushinteger(L, 0);
                return 1;
            }

            int count = RegisterSceneActors(L, domain, name);
            LuaNative.lua_pushinteger(L, count);
            return 1;
        }

        // ============================================================
        //  UnregisterActors(domain)
        // ============================================================

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_UnregisterActors(IntPtr L)
        {
            var args = new ArgReader(L, "Scene.UnregisterActors");
            args.ExpectInRange(1, 2);

            args.CheckUserdataRaw(1);

            // Заглушка — реализуй, если нужно.
            // Второй аргумент (имя сцены) опционален.
            if (args.Count >= 2)
            {
                string name = args.CheckString(2);
                _ = name;
            }

            return 0;
        }

        // ============================================================
        //  Внутреннее (не binding)
        // ============================================================

        private static bool IsExplicitNil(IntPtr L, int idx)
        {
            return LuaNative.lua_type(L, idx) == LuaNative.LUA_TNIL;
        }

        private static int RegisterSceneActors(IntPtr L, Domain domain, string sceneName)
        {
            Scene scene = string.IsNullOrEmpty(sceneName)
                ? SceneManager.GetActiveScene()
                : SceneManager.GetSceneByName(sceneName);

            if (!scene.IsValid())
            {
                LuaFail.Fail(L, "Scene.RegisterActors",
                    $"scene `{sceneName}` is not valid");
            }
            if (!scene.isLoaded)
            {
                LuaFail.Fail(L, "Scene.RegisterActors",
                    $"scene `{sceneName}` is not loaded");
            }

            // Получаем SpatialDomain для индексов.
            SpatialDomain spatialDomain = null;
            if (domain.HasSpatial)
                SpatialDomainRegistry.TryGet(domain.SpatialDomainId, out spatialDomain);

            BaseActor[] actors = UnityEngine.Object.FindObjectsByType<BaseActor>();

            int registered = 0;

            for (int i = 0; i < actors.Length; i++)
            {
                BaseActor actor = actors[i];
                if (actor == null) continue;
                if (actor.gameObject.scene.handle != scene.handle) continue;

                if (HostManager.IsValid(actor.Host))
                {
                    Debug.LogWarning(
                        $"[Scene.RegisterActors] Host already initialized for `{actor.name}` — skip");
                    continue;
                }

                Host host = HostManager.CreateHost();
                actor.LinkToHost(host);

                // --- PositionComponent ---
                Handle hPos = DCSystem.Allocate<PositionComponent>(host, domain.HostChain);
                ref var posComp = ref DCSystem.ResolveHandle<PositionComponent>(hPos);
                posComp.Position = actor.transform.position;
                posComp.Rotation = actor.transform.rotation;

                // --- NameComponent ---
                string actorName = actor.NameString;
                if (string.IsNullOrEmpty(actorName))
                {
                    LuaFail.Fail(L, "Scene.RegisterActors",
                        $"actor `{actor.name}` has empty name");
                }

                Handle hName = DCSystem.Allocate<NameComponent>(host, domain.HostChain);
                ref var nameComp = ref DCSystem.ResolveHandle<NameComponent>(hName);
                nameComp.Name = actorName;
                spatialDomain?.Names?.Add(new Name(actorName), host.Id);

                // --- ArchetypeComponent ---
                string archetype = actor.Archetype;
                if (string.IsNullOrEmpty(archetype))
                {
                    Debug.LogWarning(
                        $"[Scene.RegisterActors] actor `{actor.name}` has empty archetype — skip index");
                }
                else
                {
                    Handle hArch = DCSystem.Allocate<ArchetypeComponent>(host, domain.HostChain);
                    ref var archComp = ref DCSystem.ResolveHandle<ArchetypeComponent>(hArch);
                    archComp.Archetype = archetype;
                    spatialDomain?.Archetypes?.Add(new Name(archetype).Id, host);
                }

                registered++;
            }

            return registered;
        }
    }
}