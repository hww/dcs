using System;
using System.Runtime.CompilerServices;

public struct InvokeRecord
{
    public HostHandle ReceiverHost;       // Хост-получатель подписки
    public ComponentHandle ReceiverProcessHandle; // Хэндл Процесса-Автомата
    public int ReceiverProcessTypeId;     // TypeId компонента-процесса
    public int ComponentId;               // Индекс сообщения в плотном пуле событий (denseIndex)
    public int ComponentGeneration;       // Поколение сообщения для валидации
}

public static class EventSystem
{
    public const int MaxInvokes = 1000;
    private static readonly InvokeRecord[] _invokeList = new InvokeRecord[MaxInvokes];
    private static int _invokeCount = 0;

    /// <summary>
    /// Фаза А: Сбор и фильтрация событий по маскам пространств имен (Namespaces)
    /// </summary>
    public static void PollEvents<TEvent>(SubscriptionManager subManager) where TEvent : struct, IEventData
    {
        int eventTypeId = ComponentType<TEvent>.Id;
        
        var eventPool = (EventManager<TEvent>)ComponentRegistry.Pools[eventTypeId]; 
        if (eventPool.Partition == 0) return; 

        int subIdx = subManager.GetTypeChainHead(eventTypeId);

        while (subIdx >= 0)
        {
            ref SubscriptionNode sub = ref subManager.Components[subIdx];

            for (int j = 0; j < eventPool.Partition; j++)
            {
                ref TEvent ev = ref eventPool.Components[j];

                if ((ev.NamespaceMask & sub.NamespaceMask) != 0)
                {
                    if (_invokeCount >= _invokeList.Length) return;

                    // ИСПРАВЛЕНО: Данные хоста и поколения теперь берутся из структуры RosterItem
                    _invokeList[_invokeCount] = new InvokeRecord
                    {
                        ReceiverHost = eventPool.Roster[j].Host,
                        ReceiverProcessHandle = sub.ProcessHandle,
                        ReceiverProcessTypeId = sub.ProcessTypeId,
                        ComponentId = j, // Для покадровых событий denseIndex равен rosterIndex
                        ComponentGeneration = eventPool.Roster[j].Generation
                    };
                    _invokeCount++;
                }
            }
            subIdx = sub.NextInTypeChain;
        }
    }
    
    /// <summary>
    /// Фаза Б: Чистая полиморфная доставка БЕЗ МУСОРА И БЕЗ БОКСИНГА через SystemDeliver
    /// </summary>
    public static void DeliverEvents(Chain chain)
    {
        for (int i = 0; i < _invokeCount; i++)
        {
            InvokeRecord record = _invokeList[i];

            // Собираем полиморфный "паспорт" сообщения
            MessageHandle msgHandle = new MessageHandle
            {
                TypeId = record.ReceiverProcessTypeId,
                ComponentId = record.ComponentId,
                Generation = record.ComponentGeneration
            };

            // Получаем пул компонента-процесса (автомата), который подписался на событие
            IComponentPool targetPool = ComponentRegistry.Pools[record.ReceiverProcessTypeId];
            
            // ИСПРАВЛЕНО: Находим плотный индекс самого Процесса-Автомата внутри его пула.
            // Нам нужно достать его через хэндл процесса (ReceiverProcessHandle.Id)
            // Но так как targetPool скрыт за интерфейсом IComponentPool, нам нужен способ 
            // передать туда запрос. Самый чистый и прямой путь — достать плотный индекс 
            // из ростера этого пула, если мы приведем его к базовому ComponentManager,
            // либо вызывать доставку напрямую по хэндлу.
            // Поскольку у нас есть метод SystemDeliver(int denseIndex, MessageHandle msgHandle),
            // мы временно кастим к базовому менеджеру без привязки к конкретной T, чтобы узнать индекс:
            if (targetPool is IComponentPool pool)
            {
                // Для доставки нам нужен denseIndex компонента-приемника. 
                // Мы берем хэндл процесса, проверяем валидность поколения и достаем DenseIndex.
                // Чтобы не плодить касты, мы можем расширить SystemDeliver, передавая туда сразу 
                // ComponentHandle процесса-приемника, а пул сам внутри себя сделает быстрый Resolve.
                
                // Давай сделаем это максимально элегантно: пускай пул сам найдет свой denseIndex по Id хэндла:
                int receiverRosterId = record.ReceiverProcessHandle.Id;
                
                // Вызываем наше исправленное, легальное системное API доставки прямо в пул:
                targetPool.SystemDeliver(receiverRosterId, msgHandle);
            }
        }
        _invokeCount = 0;
    }
}
