using DCS.Lua;
using DCS.Core;
using DCS.Lua.Bindings;
using System;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // 1. Регистрируем пулы компонентов (сканирует сборки, читает BasePoolAttribute)
        ComponentRegistry.InitializeAllPools();

        // 2. Создаём домены ДО инициализации Lua-стейта,
        //    чтобы RegisterDomains увидел их и заполнил глобал Domain.
        DomainRegistry.Create("Default");
        DomainRegistry.Create("GameWorld");

        // 3. Регистрируем коллбек для Lua-биндингов
        LuaManager.RegisterBindingsCallback = RegisterAll;

        Debug.Log("[GameBootstrap] Bindings callback registered, domains created");
    }

    public static void RegisterAll(IntPtr L)
    {
        // Здесь можно зарегистрировать дополнительные биндинги поверх LuaBindings.RegisterAll.
    }
}