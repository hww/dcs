using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

// =========================================================================
// СИСТЕМНЫЕ АТРИБУТЫ ДЛЯ НАСТРОЙКИ ГЕОМЕТРИИ ПАМЯТИ ПУЛОВ
// ========================================================================

public enum EUpdateStage 
{
    None,
    Update,
    FixedUpdate,
    PostUpdate
}

public enum EAsyncUpdateStage 
{
    None,
    Update,
    FixedUpdate,
    PostUpdate
}

public static class DynamicComponentSystem
{
    public static DcsHandle Get<T>(HostHandle host_handle, HostChainManager chain) where T : struct
    {
        // ИСПРАВЛЕНО: Вместо typed.Id и typed.Generation возвращаем агрегированный хэндл typed.Component
        ChainNode typed = chain.GetTypedHandle(host_handle, ComponentType<T>.Id);
        return typed.Component;
    }

    // ИСПРАВЛЕНО: Добавлен строго типизированный вызов GetPool<T>()
    public static DcsHandle Allocate<T>(HostHandle host_handle, HostChainManager chain) where T : struct
    {
        return ComponentRegistry.GetPool<T>().Allocate(host_handle, chain);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static ref T ResolveHandle<T>(DcsHandle component_handle) where T : struct
    {
        return ref ComponentRegistry.GetPool<T>().ResolveHandle(component_handle);
    }

    // ИСПРАВЛЕНО: Метод переведен в generic-формат Free<T> с вызовом GetPool<T>()
    public static void Free<T>(HostHandle host_handle, HostChainManager chain, ref DcsHandle component_handle) where T : struct
    {
        ComponentRegistry.GetPool<T>().Free(host_handle, chain, ref component_handle);
    }

    public static void FreeChain(HostHandle host_handle, HostChainManager chain)
    {
        chain.FreeChain(host_handle);
    }

 public static void UpdateComponents(EUpdateStage stage, SubscriptionManager subManager, TypeChainManager typeChain, HostChainManager chain, uint mask = 0)
    {
        switch (stage)
        {
            case EUpdateStage.Update:
                // ФАЗА 1: Собираем события по всему реестру
                for (int i = 0; i < ComponentRegistry.PollTypesCount; i++)
                {
                    int eventTypeId = ComponentRegistry.PollTypeIds[i];
                    
                    if (ComponentRegistry.Pools[eventTypeId] is IEventDispatcher dispatcher)
                    {
                        // ИСПРАВЛЕНО: передаем оба менеджера в пулы
                        dispatcher.SystemPoll(subManager, typeChain);
                    }
                }

                // ФАЗА 2: Пакетная полиморфная доставка
                EventSystem.DeliverEvents(chain);
                break;

            case EUpdateStage.PostUpdate:
                // В PostUpdate мгновенно сбрасываем покадровые пулы.
                for (int i = 0; i < ComponentRegistry.PollTypesCount; i++)
                {
                    int eventTypeId = ComponentRegistry.PollTypeIds[i];
                    ComponentRegistry.Pools[eventTypeId].ClearFramePool();
                }
                break;
        }
    }
}
    