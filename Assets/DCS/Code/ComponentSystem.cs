using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

// =========================================================================
// СИСТЕМНЫЕ АТРИБУТЫ ДЛЯ НАСТРОЙКИ ГЕОМЕТРИИ ПАМЯТИ ПУЛОВ
// ========================================================================

// Маркеры для фильтрации типов компилятором
public interface IComponentData { }
public interface IEventData {
    // ИСПРАВЛЕНО: Маска переехала сюда! Теперь EventManager и EventSystem легально видят это поле
    uint NamespaceMask { get; set; } 
}

// Интерфейс для вашей идеи с чистой инициализацией компонентов
public interface IDcsInitializable
{
    void Init(object  prius);
}

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

public enum EAsyncUpdateStage 
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
        // ИСПРАВЛЕНО: Вместо тяжелого typeof(T) мгновенно пробрасываем сгенерированный int ID типа!
        ChainNode typed = chain.GetTypedHandle(host_handle, ComponentType<T>.Id);
        return new ComponentHandle { Id = typed.Id, Generation = typed.Generation };
    }

    // ИСПРАВЛЕНО: Добавлен строго типизированный вызов GetPool<T>()
    public static ComponentHandle Allocate<T>(HostHandle host_handle, Chain chain) where T : struct
    {
        return ComponentRegistry.GetPool<T>().Allocate(host_handle, chain);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static ref T ResolveHandle<T>(ComponentHandle component_handle) where T : struct
    {
        return ref ComponentRegistry.GetPool<T>().ResolveHandle(component_handle);
    }

    // ПУНКТ 2: Добавляем перегрузку для Варианта Б (Устраняет CS1503 и CS0103)
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static ref T ResolveHandle<T>(MessageHandle msgHandle) where T : struct
    {
        ComponentManager<T> pool = ComponentRegistry.GetPool<T>();
        
        // ИСПРАВЛЕНО: Теперь все имена переменных строго соответствуют аргументу msgHandle!
        if (pool.Roster[msgHandle.ComponentId].Generation == msgHandle.Generation)
        {
            int denseIndex = pool.Roster[msgHandle.ComponentId].Index;
            return ref pool.Components[denseIndex];
        }

        throw new System.InvalidCastException($"DCS ValidCast Error: Хэндл сообщения устарел для пула {typeof(T).Name}");
    }

    // ИСПРАВЛЕНО: Метод переведен в generic-формат Free<T> с вызовом GetPool<T>()
    public static void Free<T>(HostHandle host_handle, Chain chain, ref ComponentHandle component_handle) where T : struct
    {
        ComponentRegistry.GetPool<T>().Free(host_handle, chain, ref component_handle);
    }

    public static void FreeChain(HostHandle host_handle, Chain chain)
    {
        chain.FreeChain(host_handle);
    }

    public static void UpdateComponents(EUpdateStage stage, SubscriptionManager subManager, Chain chain, uint mask = 0)
    {
        switch (stage)
        {
            case EUpdateStage.Update:
                EventSystem.PollEvents<LocationEvent>(subManager);
                break;

            case EUpdateStage.PostUpdate:
                EventSystem.DeliverEvents(chain);

                // ИСПРАВЛЕНО: Никакой рефлексии в цикле очистки кадров! Прямой вызов интерфейса
                for (int i = 0; i < ComponentRegistry.PollTypesCount; i++)
                {
                    ComponentRegistry.Pools[ComponentRegistry.PollTypeIds[i]].ClearFramePool();
                }
                break;
        }
    }
}
    