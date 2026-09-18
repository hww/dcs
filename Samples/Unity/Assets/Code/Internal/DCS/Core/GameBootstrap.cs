using DCS.Lua;
using DCS.Lua.Bindings;
using System;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        LuaManager.RegisterBindingsCallback = RegisterAll;
        Debug.Log("[GameBootstrap] Bindings callback registered");
    }

    public static void RegisterAll(IntPtr L)
    {
        // Биндинги игры
        

    }
}