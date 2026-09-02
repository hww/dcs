using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

// Теперь это единая структура ноды связи (бывший ComponentTypedHandle)
public struct ChainNode 
{
    // Внутри инкапсулированы int Id и int Generation
    public ComponentHandle Component; 
    public int TypeId;      
    public int Next; // Индекс следующей ноды в массиве HostChainManager
    
    public bool IsNull => Component.IsNull;
}



public class HostChainManager 
{
    public const int MaxComponents = 500000; 
    private readonly ChainNode[] _components = new ChainNode[MaxComponents];
    private int _firstFree;

    public HostChainManager() 
    {
        _firstFree = 0;
        for (int i = 0; i < MaxComponents; i++) 
        {
            _components[i] = new ChainNode { Next = i + 1 }; 
            _components[i].Component.Id = -1;
        }
        _components[MaxComponents - 1].Next = -1;
    }

    // Сигнатура теперь принимает чистые хэндлы вместо мешанины int параметров!
    public void Add(HostHandle host, ComponentHandle component, int typeId) 
    {
        if (!HostManager.IsValid(host)) return;
        if (Contains(host, component, typeId)) return;
        
        if (_firstFree == -1) throw new System.Exception("DCS Error: Закончилась память под ChainNode!");

        int allocatedNodeIndex = _firstFree;
        _firstFree = _components[allocatedNodeIndex].Next;

        ref ChainNode node = ref _components[allocatedNodeIndex];
        node.Component = component; // Пишем хэндл одной операцией
        node.TypeId = typeId;

        // Вариант А: Модифицируем заголовок списка прямо в HostManager
        ref DCHost globalHost = ref HostManager.GlobalHosts[host.Id];
        node.Next = globalHost.FirstComponent; 
        globalHost.FirstComponent = allocatedNodeIndex; 
    }

    public void Remove(HostHandle host, ComponentHandle component, int typeId) 
    {
        if (!HostManager.IsValid(host)) return;

        ref DCHost globalHost = ref HostManager.GlobalHosts[host.Id];
        int currentIndex = globalHost.FirstComponent;
        int previousIndex = -1;

        while (currentIndex >= 0) 
        {
            ref ChainNode node = ref _components[currentIndex];

            // Красивое атомарное сравнение хэндлов
            if (node.Component.Id == component.Id && node.TypeId == typeId) 
            {
                if (previousIndex == -1) globalHost.FirstComponent = node.Next;
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
    public bool Contains(HostHandle host, ComponentHandle component, int typeId) 
    {
        if (!HostManager.IsValid(host)) return false;

        int currentIndex = HostManager.GlobalHosts[host.Id].FirstComponent;
        while (currentIndex >= 0) 
        {
            ref ChainNode node = ref _components[currentIndex];
            if (node.Component.Id == component.Id && node.TypeId == typeId) return true;
            currentIndex = node.Next;
        }
        return false;
    }

    public ChainNode GetTypedHandle(HostHandle host, int typeId)
    {
        if (!HostManager.IsValid(host)) return default;

        int currentIndex = HostManager.GlobalHosts[host.Id].FirstComponent;
        while (currentIndex >= 0) 
        {
            ref ChainNode node = ref _components[currentIndex];
            if (node.TypeId == typeId) return node;
            currentIndex = node.Next;
        }
        return default;
    }

    public void FreeChain(HostHandle host)
    {
        if (!HostManager.IsValid(host)) return;

        ref DCHost globalHost = ref HostManager.GlobalHosts[host.Id];
        int currentIndex = globalHost.FirstComponent;

        while (currentIndex >= 0)
        {
            ref ChainNode node = ref _components[currentIndex];
            
            // Запрашиваем пул по TypeId
            IComponentPool pool = ComponentRegistry.Pools[node.TypeId];
            
            // Системное освобождение. Мы избавляемся от передачи 'this' в пулы, 
            // передавая ссылку на HostChainManager, если пулу все еще нужно вызвать Remove.
            pool.SystemFree(host, this, node.Component.Id);

            int nextIndex = node.Next;
            node = default;
            _components[currentIndex].Next = _firstFree;
            _firstFree = currentIndex;

            currentIndex = nextIndex;
        }

        globalHost.FirstComponent = -1;
        HostManager.Invalidate(host); // Смерть хоста делегирована HostManager
    }
}