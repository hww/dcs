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

// Добавляем маркерный контракт IDcsComponent для автоматической прописи RosterIndex
public struct SubscriptionNode : IComponentData, IDcsComponent
{
    public int TargetEventTypeId;        
    public ComponentHandle ProcessHandle; 
    public int ProcessTypeId;            
    public uint NamespaceMask;           
    public int NextInTypeChain;          
    
    // Поле обратной связи, требуемое новым контрактом ComponentManager
    public int RosterIndex { get; set; } 
}

// =========================================================================
// РЕАКТИВНЫЙ МЕНЕДЖЕР ПОДПИСОК (БЕЗ ВИРТУАЛЬНЫХ ВЫЗОВОВ И VTABLE)
// =========================================================================
public class SubscriptionManager : ComponentManager<SubscriptionNode>
{
    private readonly int[] _typeChainFirst = new int[ComponentRegistry.MaxComponentTypes];

    public SubscriptionManager(int capacity) : base(capacity, EUpdateStage.Update, EAsyncUpdateStage.None, 0)
    {
        for (int i = 0; i < _typeChainFirst.Length; i++) _typeChainFirst[i] = -1;
    }

    public ComponentHandle AllocateSubscription<TEvent, TProcess>(
        HostHandle receiverHost, ComponentHandle receiverProcessHandle, uint namespaceMask, Chain chain)
        where TEvent : struct, IEventData
        where TProcess : struct, IComponentData, IDcsMessageReceiver
    {
        ComponentHandle subHandle = base.Allocate(receiverHost, chain);
        
        // Извлекаем плотный индекс выделенного элемента
        int denseIndex = Partition - 1;
        ref SubscriptionNode node = ref Components[denseIndex];

        int eventTypeId = ComponentType<TEvent>.Id;
        node.TargetEventTypeId = eventTypeId;
        node.ProcessHandle = receiverProcessHandle;
        node.ProcessTypeId = ComponentType<TProcess>.Id;
        node.NamespaceMask = namespaceMask;

        // Встраиваем подписку в начало цепочки данного типа событий
        node.NextInTypeChain = _typeChainFirst[eventTypeId];
        _typeChainFirst[eventTypeId] = denseIndex;

        return subHandle;
    }

    public void FreeSubscription(HostHandle hostHandle, Chain chain, ref ComponentHandle componentHandle)
    {
        int rosterIndexToDelete = componentHandle.Id;
        
        // ИСПРАВЛЕНО: Индекс достается через структуру RosterItem
        int denseIndexToDelete = Roster[rosterIndexToDelete].Index;
        int eventTypeId = Components[denseIndexToDelete].TargetEventTypeId;

        // ЭТАП 1: Вырезаем удаляемый dense-индекс из цепочки событий _typeChainFirst
        int currentIndex = _typeChainFirst[eventTypeId];
        int previousIndex = -1;

        while (currentIndex >= 0)
        {
            if (currentIndex == denseIndexToDelete)
            {
                if (previousIndex == -1) _typeChainFirst[eventTypeId] = Components[currentIndex].NextInTypeChain;
                else Components[previousIndex].NextInTypeChain = Components[currentIndex].NextInTypeChain;
                break;
            }
            previousIndex = currentIndex;
            currentIndex = Components[previousIndex].NextInTypeChain;
        }

        // Вызываем базовое освобождение памяти и рокировку Swap-Back
        base.Free(hostHandle, chain, ref componentHandle);
        
        // ЭТАП 2: Корректировка индексов в цепочках из-за сдвига памяти Swap-Back
        // Элемент, который лежал в самом хвосте (индекс равен Partition), уехал на место denseIndexToDelete
        int denseIndexMoved = Partition; 

        if (denseIndexToDelete != denseIndexMoved)
        {
            // Берем тип события элемента, который физически переместился в памяти
            int movedEventTypeId = Components[denseIndexToDelete].TargetEventTypeId;
            
            int curr = _typeChainFirst[movedEventTypeId];
            int prev = -1;

            // Нам нужно найти старое упоминание denseIndexMoved в связном списке и переписать его на denseIndexToDelete
            while (curr >= 0)
            {
                if (curr == denseIndexMoved)
                {
                    if (prev == -1) _typeChainFirst[movedEventTypeId] = denseIndexToDelete;
                    else Components[prev].NextInTypeChain = denseIndexToDelete;
                    break;
                }
                prev = curr;
                curr = Components[prev].NextInTypeChain;
            }

            // Защита от зацикливания: если элемент ссылался сам на себя во время рокировки
            if (Components[denseIndexToDelete].NextInTypeChain == denseIndexMoved)
            {
                Components[denseIndexToDelete].NextInTypeChain = denseIndexToDelete;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetTypeChainHead(int eventTypeId)
    {
        return _typeChainFirst[eventTypeId];
    }

    public override void ClearFramePool()
    {
        base.ClearFramePool();
        for (int i = 0; i < _typeChainFirst.Length; i++) _typeChainFirst[i] = -1;
    }
}
