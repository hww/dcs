using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

// =========================================================================
// СИСТЕМНЫЕ СТРУКТУРЫ И ИНТЕРФЕЙСЫ ЯДРА СИСТЕМЫ СООБЩЕНИЙ
// =========================================================================

public struct MessageHandle
{
    public int TypeId;       
    public int ComponentId;  
    public int Generation;   
}

public interface IDcsMessageReceiver
{
    void ReceiveMessage(MessageHandle msgHandle);
}

public struct SubscriptionNode : IComponentData, IDcsComponent
{
    public int TargetEventTypeId;        
    public ComponentHandle ProcessHandle; 
    public int ProcessTypeId;            
    public uint NamespaceMask;           
    
    // ИСПРАВЛЕНО: Поле NextInTypeChain удалено! Чейном типов теперь рулит TypeChainManager
    
    public int RosterIndex { get; set; } 
}

// =========================================================================
// РЕАКТИВНЫЙ МЕНЕДЖЕР ПОДПИСОК (БЕЗ ВИРТУАЛЬНЫХ ВЫЗОВОВ И VTABLE)
// =========================================================================
public class SubscriptionManager : ComponentManager<SubscriptionNode>
{
    // ИСПРАВЛЕНО: Массив _typeChainFirst удален, логика изолирована в TypeChainManager

    public SubscriptionManager(int capacity) : base(capacity, EUpdateStage.Update, EAsyncUpdateStage.None, 0)
    {
    }

    public ComponentHandle AllocateSubscription<TEvent, TProcess>(
        HostHandle receiverHost, 
        ComponentHandle receiverProcessHandle, 
        uint namespaceMask, 
        HostChainManager hostChain, 
        TypeChainManager typeChain)
        where TEvent : struct, IEventData
        where TProcess : struct, IComponentData, IDcsMessageReceiver
    {
        // 1. Выделяем подписку как компонент хоста (для очистки при смерти автомата)
        ComponentHandle subHandle = base.Allocate(receiverHost, hostChain);
        
        int denseIndex = Partition - 1;
        ref SubscriptionNode node = ref Components[denseIndex];

        int eventTypeId = ComponentType<TEvent>.Id;
        node.TargetEventTypeId = eventTypeId;
        node.ProcessHandle = receiverProcessHandle;
        node.ProcessTypeId = ComponentType<TProcess>.Id;
        node.NamespaceMask = namespaceMask;

        // 2. ВОТ ОНО: Просто прописываем стабильный хэндл подписки в цепь типа событий!
        typeChain.Add(eventTypeId, subHandle, node.ProcessTypeId);

        return subHandle;
    }

    public void FreeSubscription(HostHandle hostHandle, HostChainManager hostChain, TypeChainManager typeChain, ref ComponentHandle componentHandle)
    {
        int rosterIndexToDelete = componentHandle.Id;
        int denseIndexToDelete = Roster[rosterIndexToDelete].Index;
        int eventTypeId = Components[denseIndexToDelete].TargetEventTypeId;

        // ЭТАП 1: Вырезаем ноду подписки из изолированного менеджера цепей типов за O(N_подписок_типа)
        typeChain.Remove(eventTypeId, componentHandle);

        // ЭТАП 2: Просто вызываем базовый Free. Рокировка Swap-Back двигает память,
        // но благодаря IDcsComponent ростер мгновенно обновляет DenseIndex, а хэндлы в TypeChainNode не ломаются!
        base.Free(hostHandle, hostChain, ref componentHandle);
        
        // Никакой ручной перестройки индексов в связных списках больше нет!
    }

    public override void ClearFramePool()
    {
        base.ClearFramePool();
        // Очистка цепей типов делегирована фазе кадра или самому TypeChainManager, если пулы покадровые
    }
}
