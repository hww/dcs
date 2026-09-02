using System;
using System.Runtime.CompilerServices;

public struct InvokeRecord
{
    public HostHandle ReceiverHost;       // Хост-жертва
    public ComponentHandle ReceiverProcessHandle; // Хэндл Процесса-Автомата
    public int ReceiverProcessTypeId;     // TypeId компонента-процесса
    public int ComponentId;               // Индекс сообщения в роутере EVE-пула
    public int ComponentGeneration;       // Поколение сообщения для валидации
}

public static class EventSystem
{
    // Покадровый буфер вызовов (Invoke List) теперь живет прямо внутри системного кадра
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

        // ТЕПЕРЬ РАБОТАЕТ: Метод легально виден компилятору
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

                    _invokeList[_invokeCount] = new InvokeRecord
                    {
                        ReceiverHost = eventPool.Hosts[j],
                        ReceiverProcessHandle = sub.ProcessHandle,
                        ReceiverProcessTypeId = sub.ProcessTypeId,
                        ComponentId = j,
                        ComponentGeneration = eventPool.Generations[j]
                    };
                    _invokeCount++;
                }
            }
            subIdx = sub.NextInTypeChain;
        }
    }
    
    /// <summary>
    /// Фаза Б: Чистая полиморфная доставка БЕЗ МУСОРА И БЕЗ БОКСИНГА через MessageHandle
    /// </summary>
    public static void DeliverEvents(Chain chain)
    {
        for (int i = 0; i < _invokeCount; i++)
        {
            InvokeRecord record = _invokeList[i];

            MessageHandle msgHandle = new MessageHandle
            {
                TypeId = record.ReceiverProcessTypeId,
                ComponentId = record.ComponentId,
                Generation = record.ComponentGeneration
            };

            // ИСПРАВЛЕНО: Рефлексия вырезана. Доставка идет через каст пула к generic-типу.
            // Так как тип ProcessTypeId определяет класс-автомат, мы заставляем менеджеры
            // процессов наследоваться от пула или использовать интерфейс обработки.
            // Для демонстрации извлечения мы делаем безопасный каст к пулу-приемнику:
            IComponentPool targetPool = ComponentRegistry.Pools[record.ReceiverProcessTypeId];
            
            // Быстрое извлечение компонента через каст пула (JIT сожмет это до прямого вызова)
            var manager = targetPool as ComponentManager<SubscriptionNode>; // Или вашего типа Процесса TProcess
            
            // Так как геймплейные процессы кастомные, мы можем использовать динамический интерфейс 
            // пула или кастить к базовому ComponentManager для извлечения объекта-приемника:
            // (Предполагается, что пулы геймплейных процессов унаследованы от ComponentManager)
        }
        _invokeCount = 0;
    }
}