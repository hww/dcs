using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

// =========================================================================
// СИСТЕМНЫЕ АТРИБУТЫ ДЛЯ НАСТРОЙКИ ГЕОМЕТРИИ ПАМЯТИ ПУЛОВ
// ========================================================================

// Игровой объект (абсолютно невесом)
public struct DCHost 
{
    public int Id;
    public int Generation;
    public int FirstComponent;
}

public struct HostHandle 
{
    public int Id;
    public int Generation;
    public bool IsNull => Generation == 0; 
}

public struct ComponentHandle 
{
    public int Id; // Индекс в массиве пула
    public int Generation;
    public bool IsNull => Generation == 0;
}

public enum EUpdateStage 
{
    None,
    Update,
    FixedUpdate,
    PostUpdate
}

public static class DynamicComponentSystem
{
    public static ComponentHandle Get<T>(HostHandle host_handle, Chain chain) where T : struct
    {
        ChainNode typed = chain.GetTypedHandle(host_handle, typeof(T));
        return new ComponentHandle { Id = typed.Id, Generation = typed.Generation };
    }

    // ИСПРАВЛЕНО: Добавлен строго типизированный вызов GetPool<T>()
    public static ComponentHandle Allocate<T>(HostHandle host_handle, ref Chain chain, object prius = null) where T : struct
    {
        return ComponentRegistry.GetPool<T>().Allocate(host_handle, ref chain, prius);
    }

    // ИСПРАВЛЕНО: Добавлен строго типизированный вызов GetPool<T>()
    public static ref T ResolveHandle<T>(ComponentHandle component_handle) where T : struct
    {
        return ref ComponentRegistry.GetPool<T>().ResolveHandle(component_handle);
    }

    // ИСПРАВЛЕНО: Метод переведен в generic-формат Free<T> с вызовом GetPool<T>()
    public static void Free<T>(HostHandle host_handle, ref Chain chain, ref ComponentHandle component_handle) where T : struct
    {
        ComponentRegistry.GetPool<T>().Free(host_handle, ref chain, ref component_handle);
    }

    // ИСПРАВЛЕНО: Добавлен строго типизированный вызов GetPool<LocationEvent>()
    public static void UpdateComponents(EUpdateStage stage, EventDispatcher dispatcher)
    {
        switch (stage)
        {
            case EUpdateStage.Update:
                dispatcher.PollSubscriptions();
                break;
            case EUpdateStage.PostUpdate:
                dispatcher.DeliverPollEvents();
                ComponentRegistry.GetPool<LocationEvent>().ClearFramePool();
                break;
        }
    }
}
    