using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

// Теперь это единая структура ноды связи (бывший ComponentTypedHandle)
public struct ChainNode 
{
    public int Id;          // Стабильный индекс компонента в его родном пуле
    public int Generation;  // Поколение для валидации
    
    // ИСПРАВЛЕНО (TODO 3): Вместо тяжелого System.Type храним быстрый целочисленный ID типа
    public int TypeId;      
    
    public int Next;        // Индекс СЛЕДУЮЩЕЙ ноды в глобальном массиве Chain (или -1)
    
    public bool IsNull => Generation == 0;
}


public class Chain 
{
    public const int MaxGameObjects = 100000;
    public const int MaxComponents = 500000; 

    public static readonly DCHost[] GlobalHosts = new DCHost[MaxGameObjects];
    private readonly ChainNode[] _components = new ChainNode[MaxComponents];
    private int _firstFree;

    public Chain() 
    {
        _firstFree = 0;
        for (int i = 0; i < MaxComponents; i++) 
        {
            _components[i] = new ChainNode { Next = i + 1, Id = -1 }; 
        }
        _components[MaxComponents - 1].Next = -1;

        for (int i = 0; i < MaxGameObjects; i++)
        {
            GlobalHosts[i] = new DCHost { Id = i, Generation = 1, FirstComponent = -1 };
        }
    }

    public void Add(HostHandle hostHandle, int componentId, int typeId, int generation) 
    {
        if (GlobalHosts[hostHandle.Id].Generation != hostHandle.Generation) return;
        if (Contains(hostHandle, componentId, typeId)) return;
        
        if (_firstFree == -1) throw new System.Exception("DCS Error: Закончилась память под ChainNode!");

        int allocatedNodeIndex = _firstFree;
        _firstFree = _components[allocatedNodeIndex].Next;

        ref ChainNode node = ref _components[allocatedNodeIndex];
        node.Id = componentId;
        node.Generation = generation;
        node.TypeId = typeId;

        ref DCHost host = ref GlobalHosts[hostHandle.Id];
        node.Next = host.FirstComponent; 
        host.FirstComponent = allocatedNodeIndex; 
    }

    public void Remove(HostHandle hostHandle, int componentId, int typeId) 
    {
        if (GlobalHosts[hostHandle.Id].Generation != hostHandle.Generation) return;

        ref DCHost host = ref GlobalHosts[hostHandle.Id];
        int currentIndex = host.FirstComponent;
        int previousIndex = -1;

        while (currentIndex >= 0) 
        {
            ref ChainNode node = ref _components[currentIndex];

            if (node.Id == componentId && node.TypeId == typeId) 
            {
                if (previousIndex == -1) host.FirstComponent = node.Next;
                else _components[previousIndex].Next = node.Next;

                node = default; 
                _components[currentIndex].Next = _firstFree;
                _firstFree = currentIndex;
                return;
            }
            previousIndex = currentIndex;
            currentIndex = node.Next;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(HostHandle hostHandle, int componentId, int typeId) 
    {
        if (GlobalHosts[hostHandle.Id].Generation != hostHandle.Generation) return false;

        int currentIndex = GlobalHosts[hostHandle.Id].FirstComponent;
        while (currentIndex >= 0) 
        {
            ref ChainNode node = ref _components[currentIndex];
            if (node.Id == componentId && node.TypeId == typeId) return true;
            currentIndex = node.Next;
        }
        return false;
    }

    public ChainNode GetTypedHandle(HostHandle hostHandle, int typeId)
    {
        if (GlobalHosts[hostHandle.Id].Generation != hostHandle.Generation) return default;

        int currentIndex = GlobalHosts[hostHandle.Id].FirstComponent;
        while (currentIndex >= 0) 
        {
            ref ChainNode node = ref _components[currentIndex];
            if (node.TypeId == typeId) return node;
            currentIndex = node.Next;
        }
        return default;
    }

    // ИСПРАВЛЕНО: Рефлексия полностью вырезана! Вызов идет через интерфейс IComponentPool
public void FreeChain(HostHandle hostHandle)
{
    if (GlobalHosts[hostHandle.Id].Generation != hostHandle.Generation) return;

    ref DCHost host = ref GlobalHosts[hostHandle.Id];
    int currentIndex = host.FirstComponent;

    while (currentIndex >= 0)
    {
        ref ChainNode node = ref _components[currentIndex];
        
        IComponentPool pool = ComponentRegistry.Pools[node.TypeId];
        
        // ИСПРАВЛЕНО: Передаем просто 'this' без модификатора ref
        pool.SystemFree(hostHandle, this, node.Id);

        int nextIndex = node.Next;
        node = default;
        _components[currentIndex].Next = _firstFree;
        _firstFree = currentIndex;

        currentIndex = nextIndex;
    }

    host.FirstComponent = -1;
    host.Generation++; 
}
}
