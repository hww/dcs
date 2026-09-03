using System.Runtime.CompilerServices;

public struct TypeChainNode
{
    public DcsHandle SubscriptionHandle; // Хэндл структуры подписки в SubscriptionManager
    public int ProcessTypeId;                 // TypeId пула Процесса-Автомата (приемника)
    public int Next;                          // Индекс следующей подписки в этой цепи типов
}

public class TypeChainManager
{
    public const int MaxTypeNodes = 100000; 
    
    private readonly TypeChainNode[] _nodes = new TypeChainNode[MaxTypeNodes];
    private readonly int[] _typeChains = new int[ComponentRegistry.MaxComponentTypes];
    private int _firstFree;

    public TypeChainManager()
    {
        _firstFree = 0;
        for (int i = 0; i < MaxTypeNodes; i++)
        {
            _nodes[i] = new TypeChainNode { Next = i + 1 };
            _nodes[i].SubscriptionHandle.Id = -1;
        }
        _nodes[MaxTypeNodes - 1].Next = -1;

        for (int i = 0; i < _typeChains.Length; i++) 
            _typeChains[i] = -1;
    }

    /// <summary>
    /// Линкует подписку в чейн конкретного типа события
    /// </summary>
    public void Add(int eventTypeId, DcsHandle subHandle, int processTypeId)
    {
        if (_firstFree == -1) 
            throw new System.Exception("DCS Error: Закончилась память в TypeChainManager!");

        int allocatedNodeIndex = _firstFree;
        _firstFree = _nodes[allocatedNodeIndex].Next;

        ref TypeChainNode node = ref _nodes[allocatedNodeIndex];
        node.SubscriptionHandle = subHandle;
        node.ProcessTypeId = processTypeId;

        node.Next = _typeChains[eventTypeId];
        _typeChains[eventTypeId] = allocatedNodeIndex;
    }

    /// <summary>
    /// Удаляет подписку из чейна конкретного типа события
    /// </summary>
    public void Remove(int eventTypeId, DcsHandle subHandle)
    {
        int currentIndex = _typeChains[eventTypeId];
        int previousIndex = -1;

        while (currentIndex >= 0)
        {
            ref TypeChainNode node = ref _nodes[currentIndex];

            if (node.SubscriptionHandle.Id == subHandle.Id)
            {
                // Вырезаем ноду из связного списка типа
                if (previousIndex == -1) _typeChains[eventTypeId] = node.Next;
                else _nodes[previousIndex].Next = node.Next;

                // Сбрасываем и возвращаем в Free List
                node = default;
                _nodes[currentIndex].Next = _firstFree;
                _firstFree = currentIndex;
                return;
            }

            previousIndex = currentIndex;
            currentIndex = node.Next;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetTypeChainHead(int eventTypeId)
    {
        return _typeChains[eventTypeId];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TypeChainNode GetNode(int index)
    {
        return ref _nodes[index];
    }
}
