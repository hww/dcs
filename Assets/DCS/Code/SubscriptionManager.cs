using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;


// =========================================================================
// СИСТЕМНЫЕ СТРУКТУРЫ И ИНТЕРФЕЙСЫ ЯДРА СИСТЕМЫ СООБЩЕНИЙ
// =========================================================================

// Полиморфный "паспорт" любого покадрового сообщения
public struct MessageHandle
{
    public int TypeId;       // ComponentType<T>.Id
    public int ComponentId;  // Индекс в Roster пула сообщений
    public int Generation;   // Поколение для валидации
}

// Контракт для любого компонента-процесса/автомата, принимающего сообщения
public interface IDcsMessageReceiver
{
    // Фиксированная сигнатура гарантирует 0 боксинга и 0 мусора при доставке
    void ReceiveMessage(MessageHandle msgHandle);
}

// Единая нода связи внутри пула подписок (Чистый линк Тип -> Процесс)
public struct SubscriptionNode : IComponentData
{
    public int TargetEventTypeId;        // На какой ТИП сообщения подписаны (ComponentType<T>.Id)
    public ComponentHandle ProcessHandle; // Хэндл структуры-Процесса в её родном пуле
    public int ProcessTypeId;            // TypeId пула этого Процесса
    public uint NamespaceMask;           // Битфилд-маска пространств имен (каналов) кадра
    public int NextInTypeChain;          // Ссылка на следующую подписку ЭТОГО ЖЕ типа события (-1 если конец)
}

// =========================================================================
// РЕАКТИВНЫЙ МЕНЕДЖЕР ПОДПИСОК (БЕЗ ВИРТУАЛЬНЫХ ВЫЗОВОВ И VTABLE)
// =========================================================================
public class SubscriptionManager : ComponentManager<SubscriptionNode>
{
    private readonly int[] _typeChainFirst = new int[ComponentRegistry.MaxComponentTypes];

    public SubscriptionManager(int capacity) : base(capacity)
    {
        for (int i = 0; i < _typeChainFirst.Length; i++) _typeChainFirst[i] = -1;
    }

    public ComponentHandle AllocateSubscription<TEvent, TProcess>(
        HostHandle receiverHost, ComponentHandle receiverProcessHandle, uint namespaceMask, Chain chain)
        where TEvent : struct, IEventData
        where TProcess : struct, IComponentData, IDcsMessageReceiver
    {
        ComponentHandle subHandle = base.Allocate(receiverHost, chain);
        int denseIndex = Partition - 1;
        ref SubscriptionNode node = ref Components[denseIndex];

        int eventTypeId = ComponentType<TEvent>.Id;
        node.TargetEventTypeId = eventTypeId;
        node.ProcessHandle = receiverProcessHandle;
        node.ProcessTypeId = ComponentType<TProcess>.Id;
        node.NamespaceMask = namespaceMask;

        node.NextInTypeChain = _typeChainFirst[eventTypeId];
        _typeChainFirst[eventTypeId] = denseIndex;

        return subHandle;
    }

    // ИСПРАВЛЕНО: убран ref у параметра Chain
    public void FreeSubscription(HostHandle hostHandle, Chain chain, ref ComponentHandle componentHandle)
    {
        int rosterIndexToDelete = componentHandle.Id;
        int denseIndexToDelete = Roster[rosterIndexToDelete];
        int eventTypeId = Components[denseIndexToDelete].TargetEventTypeId;

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

        base.Free(hostHandle, chain, ref componentHandle);
        int denseIndexMoved = Partition; 

        if (denseIndexToDelete != denseIndexMoved)
        {
            int movedEventTypeId = Components[denseIndexToDelete].TargetEventTypeId;
            int curr = _typeChainFirst[movedEventTypeId];
            int prev = -1;

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