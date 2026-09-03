using System;
using System.Runtime.CompilerServices;

public struct InvokeRecord
{
    public HostHandle ReceiverHost;               // Хост-получатель подписки
    public DcsHandle ReceiverProcessHandle; // Хэндл Процесса-Автомата (приемника)
    public int ReceiverProcessTypeId;             // TypeId пула компонента-процесса
    public int EventTypeId;                       // Сохраняем истинный TypeId самого события
    public int ComponentId;                       // Индекс сообщения в плотном пуле событий (denseIndex)
    public int ComponentGeneration;               // Поколение сообщения для валидации
}

public static class EventSystem
{
    public const int MaxInvokes = 1000;
    private static readonly InvokeRecord[] _invokeList = new InvokeRecord[MaxInvokes];
    private static int _invokeCount = 0;

    /// <summary>
    /// Фаза А: Сбор и фильтрация событий по маскам пространств имен (Namespaces)
    /// </summary>
    public static void PollEvents<TEvent>(SubscriptionManager subManager, TypeChainManager typeChain) where TEvent : struct, IEventData
    {
        int eventTypeId = ComponentType<TEvent>.Id;
        
        var eventPool = (EventManager<TEvent>)ComponentRegistry.Pools[eventTypeId]; 
        if (eventPool.Partition == 0) return; 

        // ИСПРАВЛЕНО: Голову чейна типов запрашиваем у изолированного менеджера цепочек типов
        int chainNodeIdx = typeChain.GetTypeChainHead(eventTypeId);

        // Итерируем по узлам TypeChainManager
        while (chainNodeIdx >= 0)
        {
            ref TypeChainNode chainNode = ref typeChain.GetNode(chainNodeIdx);
            
            // Восстанавливаем саму структуру подписки по стабильному хэндлу из пула подписок
            ref SubscriptionNode sub = ref subManager.ResolveHandle(chainNode.SubscriptionHandle);

            // Итерируем плотный пул покадровых событий
            int partitionSnapshot = eventPool.Partition;
            for (int j = 0; j < partitionSnapshot; j++)
            {
                ref TEvent ev = ref eventPool.Components[j];

                if ((ev.NamespaceMask & sub.NamespaceMask) != 0)
                {
                    if (_invokeCount >= _invokeList.Length) return;

                    _invokeList[_invokeCount] = new InvokeRecord
                    {
                        ReceiverHost = eventPool.Roster[j].Host,
                        ReceiverProcessHandle = sub.ProcessHandle,
                        ReceiverProcessTypeId = sub.ProcessTypeId,
                        EventTypeId = eventTypeId, 
                        ComponentId = j, 
                        ComponentGeneration = eventPool.Roster[j].Generation
                    };
                    _invokeCount++;
                }
            }
            
            // ИСПРАВЛЕНО: Переход к следующей подписке идет через поле Next внутри TypeChainNode
            chainNodeIdx = chainNode.Next;
        }
    }
    
    /// <summary>
    /// Фаза Б: Чистая полиморфная доставка БЕЗ МУСОРА И БЕЗ БОКСИНГА через SystemDeliver
    /// </summary>
    public static void DeliverEvents(HostChainManager chain)
    {
        for (int i = 0; i < _invokeCount; i++)
        {
            InvokeRecord record = _invokeList[i];

            DcsHandle msgHandle = new DcsHandle
            {
                Id = record.ComponentId,
                Generation = record.ComponentGeneration
            };

            IComponentPool targetPool = ComponentRegistry.Pools[record.ReceiverProcessTypeId];
            int receiverRosterId = record.ReceiverProcessHandle.Id;
            
            targetPool.SystemDeliver(receiverRosterId, record.EventTypeId, msgHandle);
        }
        _invokeCount = 0; 
    }
}