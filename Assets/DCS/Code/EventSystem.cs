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
    /// Новая реактивная фаза кадра: итерируем пул событий и доставляем их по чейну типов подписок
    /// </summary>
    public static void ProcessAndDeliverEvents<TEvent>(SubscriptionManager subManager, TypeChainManager typeChain) 
        where TEvent : struct, IEventData
    {
        int eventTypeId = ComponentType<TEvent>.Id;

        // 1. Получаем менеджер пула конкретного типа событий
        var eventPool = (EventManager<TEvent>)ComponentRegistry.Pools[eventTypeId]; 
        if (eventPool.Partition == 0) return; 

        // 2. Извлекаем голову цепочки подписок для этого ТИПА события ОДИН раз
        int subChainIndex = typeChain.GetTypeChainHead(eventTypeId);
        if (subChainIndex == -1) return; // На этот ивент никто не подписан, уходим

        // 3. Линейно перебираем все сообщения в плотном пуле кадра
        for (int j = 0; j < eventPool.Partition; j++)
        {
            ref TEvent ev = ref eventPool.Components[j];

            // Собираем паспорт сообщения для полиморфной доставки
            MessageHandle msgHandle = new MessageHandle
            {
                TypeId = eventTypeId,
                ComponentId = j,
                Generation = eventPool.Roster[j].Generation
            };

            // 4. Идем по выделенной изолированной цепочке подписок этого типа в TypeChainManager
            int currentIndex = subChainIndex;
            while (currentIndex >= 0)
            {
                ref TypeChainNode chainNode = ref typeChain.GetNode(currentIndex);
                
                // Извлекаем саму структуру подписки из её менеджера по стабильному хэндлу
                ref SubscriptionNode sub = ref subManager.ResolveHandle(chainNode.SubscriptionHandle);

                // Проверяем совпадение масок пространств имен (NamespaceMask)
                if ((ev.NamespaceMask & sub.NamespaceMask) != 0)
                {
                    // Точечная полиморфная доставка прямо в пул Процесса-Автомата
                    IComponentPool targetPool = ComponentRegistry.Pools[chainNode.ProcessTypeId];
                    
                    // Пул сам сделает мгновенную валидацию по rosterId и вызовет ReceiveMessage
                    targetPool.SystemDeliver(sub.ProcessHandle.Id, msgHandle);
                }

                currentIndex = chainNode.Next; // Сдвиг к следующей подписке в цепи типа
            }
        }
    }
}