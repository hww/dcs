using DCS.Core;
using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace DCS.Lua.Bindings
{
    /// <summary>
    /// Low-level DCS/ECS Lua API.
    /// Existing global names are intentionally preserved.
    ///
    /// Convention: Lua always passes Host values as Pack() (Id | Generation &lt;&lt; 16).
    /// Handles are also passed as Pack(). C# unpacks before touching GlobalHosts.
    /// </summary>
    public static class DcsBindings
    {
        public static void Register(IntPtr L)
        {
            // Внутренние — глобально (нужны до создания таблицы)
            LuaBindings.RegisterGlobalFunction(L, Lua_GetTypesCount, "Internal_GetTypesCount");
            LuaBindings.RegisterGlobalFunction(L, Lua_GetTypeNameById, "Internal_GetTypeNameById");

            // Остальное — в таблице DCS
            LuaNative.lua_newtable(L);
            LuaBindings.RegisterMethod(L, Lua_CreateComponent, "CreateComponent");
            LuaBindings.RegisterMethod(L, Lua_RemoveComponent, "RemoveComponent");
            LuaBindings.RegisterMethod(L, Lua_HasComponent, "HasComponent");
            LuaBindings.RegisterMethod(L, Lua_GetField, "GetField");
            LuaBindings.RegisterMethod(L, Lua_SetField, "SetField");
            LuaBindings.RegisterMethod(L, Lua_TryGetField, "TryGetField");
            LuaBindings.RegisterMethod(L, Lua_CreateHost, "CreateHost");
            LuaBindings.RegisterMethod(L, Lua_GetComponent, "GetComponent");      // <-- новое
            LuaBindings.RegisterMethod(L, Lua_Attach, "Attach");  // <-- DCS-операция
            LuaBindings.RegisterMethod(L, Lua_Spawn, "Spawn"); // <-- новое
            LuaNative.lua_setglobal(L, "DCS");
            RegisterDomains(L);
        }

        private static void RegisterDomains(IntPtr L)
        {
            LuaNative.lua_newtable(L);

            // Кэшируем счётчик — на случай, если реестр изменится во время обхода
            int count = DomainRegistry.Count;
            for (int i = 0; i < count; i++)
            {
                var domain = DomainRegistry.Get(i);
                if (domain == null)
                    continue;

                // Защита от null имени
                string name = string.IsNullOrEmpty(domain.Name)
                    ? $"domain_{i}"
                    : domain.Name;

                // table[name] = id
                // lua_setfield сам разберётся: top = value, key = name (строка C#)
                LuaNative.lua_pushinteger(L, domain.Id);
                LuaNative.lua_setfield(L, -2, name);

                // Опционально: table[i] = id (по индексу)
                // LuaNative.lua_pushinteger(L, chain.id);
                // LuaNative.lua_seti(L, -2, i);

                Debug.Log($"[GameBootstrap]   Domain[{i}] = '{name}'");
            }

            LuaNative.lua_setglobal(L, "Domain");
        }

        // ------------------------------------------------------------
        //  DCS_CreateHost() -> packedHost
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_CreateHost(IntPtr L)
        {
            Host host = HostManager.CreateHost();
            if (!HostManager.IsValid(host))
                return LuaNative.lua_error(L, "[DcsBindings] CreateHost: failed to create host");

            LuaNative.lua_pushinteger(L, host.ToLua());
            return 1;
        }

        // ------------------------------------------------------------
        //  DCS_Internal_GetTypesCount() -> int
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetTypesCount(IntPtr L)
        {
            LuaNative.lua_pushinteger(L, ComponentRegistry.GetTypesCount());
            return 1;
        }

        // ------------------------------------------------------------
        //  DCS_Internal_GetTypeNameById(id) -> string | nil
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetTypeNameById(IntPtr L)
        {
            int id = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            if (id < 0 || id >= ComponentRegistry.MaxComponentTypes)
                return LuaNative.lua_error(L,
                    $"[DcsBindings] GetTypeNameById: type id {id} out of range [0, {ComponentRegistry.MaxComponentTypes})");

            string name = ComponentRegistry.GetTypeNameById(id);
            if (string.IsNullOrEmpty(name))
                return LuaNative.lua_error(L, $"[DcsBindings] GetTypeNameById: no type registered for id {id}");

            LuaNative.lua_pushstring(L, name);
            return 1;
        }

        // ------------------------------------------------------------
        //  DCS_CreateComponent(domainId, typeId, packedHost) -> packedHandle | nil
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_CreateComponent(IntPtr L)
        {
            // 1. Читаем domainId первым аргументом из Lua
            int domainId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int typeId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            int packedHost = (int)LuaNative.lua_tointegerx(L, 3, IntPtr.Zero);

            // 2. Достаем нужный чейн из реестра миров
            var domain = DomainRegistry.Get(domainId);
            if (domain == null)
                return LuaNative.lua_error(L, $"[DcsBindings] CreateComponent: domain id {domainId} not found");

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
                return LuaNative.lua_error(L,
                    $"[DcsBindings] CreateComponent: type id {typeId} out of range [0, {ComponentRegistry.MaxComponentTypes})");

            Host host = Host.FromLua(packedHost);
            if (!HostManager.IsValid(host))
                return LuaNative.lua_error(L, $"[DcsBindings] CreateComponent: invalid host {packedHost}");

            var pool = ComponentRegistry.Pools[typeId];
            if (pool == null)
                return LuaNative.lua_error(L, $"[DcsBindings] CreateComponent: no pool for type id {typeId}");

            // 3. Пишем компонент в правильный чейн, который обновляет C# система
            Handle handle = pool.SystemAllocate(host, domain.HostChain);
            if (handle.IsNull)
                return LuaNative.lua_error(L,
                    $"[DcsBindings] CreateComponent: failed to allocate component type {typeId} for host {packedHost}");

            LuaNative.lua_pushinteger(L, handle.Pack());
            return 1;
        }

        // ------------------------------------------------------------
        //  DCS_RemoveComponent(domainId, typeId, packedHandle) -> void
        //  Handle already encodes the roster slot, so host lookup
        //  goes through the pool, not through GlobalHosts.
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_RemoveComponent(IntPtr L)
        {
            int domainId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero); // Читаем domainId
            int typeId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 3, IntPtr.Zero);

            // null handle — это no-op, не ошибка
            if (packedHandle == HandleConfig.NULL_INDEX)
                return 0;

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
                return LuaNative.lua_error(L,
                    $"[DcsBindings] RemoveComponent: type id {typeId} out of range [0, {ComponentRegistry.MaxComponentTypes})");

            var domain = DomainRegistry.Get(domainId);
            if (domain == null)
                return LuaNative.lua_error(L, $"[DcsBindings] RemoveComponent: domain id {domainId} not found");

            HostChain chain = domain.HostChain;
            if (chain == null)
                return LuaNative.lua_error(L, $"[DcsBindings] RemoveComponent: domain {domainId} has no HostChain");

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];
            if (pool == null)
                return LuaNative.lua_error(L, $"[DcsBindings] RemoveComponent: no pool for type id {typeId}");

            // Если хост не найден — компонент уже удалён, это no-op
            if (pool.TryGetHost(handle, out Host host))
                pool.SystemFree(host, chain, handle); // Освобождаем строго из этого чейна

            return 0;
        }

        // ------------------------------------------------------------
        //  DCS_HasComponent(domainId, typeId, packedHost) -> bool
        //  Предикат: false — легитимный ответ, ошибки только на невалидных аргументах.
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_HasComponent(IntPtr L)
        {
            int domainId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero); // Читаем domainId
            int typeId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            int packedHost = (int)LuaNative.lua_tointegerx(L, 3, IntPtr.Zero);

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
                return LuaNative.lua_error(L,
                    $"[DcsBindings] HasComponent: type id {typeId} out of range [0, {ComponentRegistry.MaxComponentTypes})");

            var domain = DomainRegistry.Get(domainId);
            if (domain == null)
                return LuaNative.lua_error(L, $"[DcsBindings] HasComponent: domain id {domainId} not found");

            HostChain chain = domain.HostChain;
            if (chain == null)
                return LuaNative.lua_error(L, $"[DcsBindings] HasComponent: domain {domainId} has no HostChain");

            Host host = Host.FromLua(packedHost);
            if (!HostManager.IsValid(host))
            {
                LuaNative.lua_pushboolean(L, 0);
                return 1;
            }

            ChainNode node = chain.GetTypedHandle(host, typeId);
            LuaNative.lua_pushboolean(L, node.IsNull ? 0 : 1);
            return 1;
        }

        // ------------------------------------------------------------
        //  DCS_GetField(typeId, packedHandle, fieldName) -> values...
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetField(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            string fieldName = ReadString(L, 3);

            if (packedHandle == HandleConfig.NULL_INDEX)
                return LuaNative.lua_error(L, "[DcsBindings] GetField: null handle");

            if (string.IsNullOrEmpty(fieldName))
                return LuaNative.lua_error(L, "[DcsBindings] GetField: field name is empty");

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
                return LuaNative.lua_error(L,
                    $"[DcsBindings] GetField: type id {typeId} out of range [0, {ComponentRegistry.MaxComponentTypes})");

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];
            if (pool == null)
                return LuaNative.lua_error(L, $"[DcsBindings] GetField: no pool for type id {typeId}");

            if (!pool.TryGetDenseIndex(handle, out int denseIndex))
                return LuaNative.lua_error(L,
                    $"[DcsBindings] GetField: handle {packedHandle} not found in pool {typeId}");

            int topBefore = LuaNative.lua_gettop(L);
            if (!pool.GetField(denseIndex, fieldName, L))
                return LuaNative.lua_error(L, $"[DCS Error] Field '{fieldName}' does not exist.");

            int count = LuaNative.lua_gettop(L) - topBefore;
            return count > 0 ? count : 1;
        }

        // ------------------------------------------------------------
        //  DCS_TryGetField(typeId, packedHandle, fieldName) -> ok, values...
        //  Try-вариант: false — легитимный ответ.
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_TryGetField(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            string fieldName = ReadString(L, 3);

            if (packedHandle == HandleConfig.NULL_INDEX)
                return LuaNative.lua_error(L, "[DcsBindings] TryGetField: null handle");

            if (string.IsNullOrEmpty(fieldName))
                return LuaNative.lua_error(L, "[DcsBindings] TryGetField: field name is empty");

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
                return LuaNative.lua_error(L,
                    $"[DcsBindings] TryGetField: type id {typeId} out of range [0, {ComponentRegistry.MaxComponentTypes})");

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];
            if (pool == null || !pool.TryGetDenseIndex(handle, out int denseIndex))
            {
                LuaNative.lua_pushboolean(L, 0);
                LuaNative.lua_pushnil(L);
                return 2;
            }

            int topBefore = LuaNative.lua_gettop(L);
            if (!pool.GetField(denseIndex, fieldName, L))
            {
                LuaNative.lua_pushboolean(L, 0);
                LuaNative.lua_pushnil(L);
                return 2;
            }

            int valueCount = LuaNative.lua_gettop(L) - topBefore;
            LuaNative.lua_pushboolean(L, 1);
            LuaNative.lua_rotate(L, -(valueCount + 1), 1);
            return valueCount + 1;
        }

        // ------------------------------------------------------------
        //  DCS_SetField(typeId, packedHandle, fieldName, values...) -> void
        //  Values are popped from the top of the stack by FieldExpressionFactory.
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_SetField(IntPtr L)
        {
            int typeId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int packedHandle = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            string fieldName = ReadString(L, 3);

            if (packedHandle == HandleConfig.NULL_INDEX)
                return LuaNative.lua_error(L, "[DcsBindings] SetField: null handle");

            if (string.IsNullOrEmpty(fieldName))
                return LuaNative.lua_error(L, "[DcsBindings] SetField: field name is empty");

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
                return LuaNative.lua_error(L,
                    $"[DcsBindings] SetField: type id {typeId} out of range [0, {ComponentRegistry.MaxComponentTypes})");

            Handle handle = new Handle(packedHandle);
            var pool = ComponentRegistry.Pools[typeId];
            if (pool == null)
                return LuaNative.lua_error(L, $"[DcsBindings] SetField: no pool for type id {typeId}");

            if (!pool.TryGetDenseIndex(handle, out int denseIndex))
                return LuaNative.lua_error(L,
                    $"[DcsBindings] SetField: handle {packedHandle} not found in pool {typeId}");

            if (!pool.SetField(denseIndex, fieldName, L))
                return LuaNative.lua_error(L,
                    $"[DCS Write Error] Cannot write to field '{fieldName}' on component type {typeId}.");

            return 0;
        }

        private static string ReadString(IntPtr L, int index)
        {
            IntPtr ptr = LuaNative.lua_tolstring(L, index, IntPtr.Zero);
            return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : null;
        }

        // ------------------------------------------------------------
        //  DCS_Attach(packedHost, goName) -> bool
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Attach(IntPtr L)
        {
            int packedHost = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            string goName = ReadString(L, 2);

            if (string.IsNullOrEmpty(goName))
                return LuaNative.lua_error(L, "[DcsBindings] Attach: game object name is empty");

            Host host = Host.FromLua(packedHost);
            if (!HostManager.IsValid(host))
                return LuaNative.lua_error(L, $"[DcsBindings] Attach: invalid host {packedHost}");

            bool ok = ViewService.Attach(host, goName);
            if (!ok)
                return LuaNative.lua_error(L,
                    $"[DcsBindings] Attach: failed to attach '{goName}' to host {packedHost}");

            LuaNative.lua_pushboolean(L, 1);
            return 1;
        }

        // ------------------------------------------------------------
        //  DCS_Spawn(packedHost, prefabPath) -> packedHost
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Spawn(IntPtr L)
        {
            int packedHost = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            string prefabPath = ReadString(L, 2);

            if (string.IsNullOrEmpty(prefabPath))
                return LuaNative.lua_error(L, "[DcsBindings] Spawn: prefab path is empty");

            Host host = Host.FromLua(packedHost);
            if (!HostManager.IsValid(host))
                return LuaNative.lua_error(L, $"[DcsBindings] Spawn: invalid host {packedHost}");

            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
                return LuaNative.lua_error(L, $"[DcsBindings] Can't load prefab '{prefabPath}'");

            GameObject go = UnityEngine.Object.Instantiate(prefab);

            var link = go.GetComponent<IHostReference>();
            if (link == null)
            {
                UnityEngine.Object.Destroy(go);
                return LuaNative.lua_error(L,
                    $"[DcsBindings] Can't find IHostReference in prefab '{prefabPath}'");
            }

            HostManager.LinkHostReference(host, link);

            LuaNative.lua_pushinteger(L, packedHost);
            return 1;
        }

        // ------------------------------------------------------------
        //  DCS_GetComponent(domainId, typeId, packedHost) -> packedHandle | nil
        // ------------------------------------------------------------
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_GetComponent(IntPtr L)
        {
            int domainId = (int)LuaNative.lua_tointegerx(L, 1, IntPtr.Zero);
            int typeId = (int)LuaNative.lua_tointegerx(L, 2, IntPtr.Zero);
            int packedHost = (int)LuaNative.lua_tointegerx(L, 3, IntPtr.Zero);

            var domain = DomainRegistry.Get(domainId);
            if (domain == null)
                return LuaNative.lua_error(L,
                    $"[DcsBindings] GetComponent: domain id {domainId} not found");

            HostChain chain = domain.HostChain;
            if (chain == null)
                return LuaNative.lua_error(L,
                    $"[DcsBindings] GetComponent: domain {domainId} has no HostChain");

            if (typeId < 0 || typeId >= ComponentRegistry.MaxComponentTypes)
                return LuaNative.lua_error(L,
                    $"[DcsBindings] GetComponent: type id {typeId} out of range");

            Host host = Host.FromLua(packedHost);
            if (!HostManager.IsValid(host))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            ChainNode node = chain.GetTypedHandle(host, typeId);
            if (node.IsNull)
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            // Проверяем, что handle не протух (компонент ещё жив в пуле)
            IComponentPool pool = ComponentRegistry.Pools[typeId];
            if (pool == null || !pool.TryGetDenseIndex(node.Component, out _))
            {
                LuaNative.lua_pushnil(L);
                return 1;
            }

            LuaNative.lua_pushinteger(L, node.Component.Pack());
            return 1;
        }
    }
}