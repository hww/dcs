using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

// Теперь это единая структура ноды связи (бывший ComponentTypedHandle)
public struct ChainNode 
{
    public int Id;          // Стабильный индекс компонента в его родном пуле
    public int Generation;  // Поколение для валидации
    public System.Type Type;// Тип компонента (System.Type)
    public int Next;        // Индекс СЛЕДУЮЩЕЙ ноды в глобальном массиве Chain (или -1)
    
    public bool IsNull => Generation == 0;
}


public class Chain 
{
    public const int MaxGameObjects = 100000;
    public const int MaxComponents = 500000; 

    // СИСТЕМНЫЙ МАССИВ ВСЕХ ИГРОВЫХ ОБЪЕКТОВ (Hosts)
    public static readonly DCHost[] GlobalHosts = new DCHost[MaxGameObjects];

    private readonly ChainNode[] _components = new ChainNode[MaxComponents];
    private int _firstFree;

    public Chain() 
    {
        _firstFree = 0;
        
        // Инициализируем FreeList нод связей
        for (int i = 0; i < MaxComponents; i++) 
        {
            _components[i] = new ChainNode { Next = i + 1, Id = -1 }; 
        }
        _components[MaxComponents - 1].Next = -1;

        // Инициализируем массив хостов (изначально у всех цепочки пусты)
        for (int i = 0; i < MaxGameObjects; i++)
        {
            GlobalHosts[i] = new DCHost { Id = i, Generation = 1, FirstComponent = -1 };
        }
    }

    // Добавление компонента в цепочку за O(1) БЕЗ ЛИМИТОВ
    public void Add(HostHandle hostHandle, int componentId, System.Type type, int generation) 
    {
        // 1. Проверяем дубликаты
        if (Contains(hostHandle, componentId, type)) return;
        
        // 2. Забираем чистую ноду из FreeList
        if (_firstFree == -1) throw new System.Exception("DCS Error: Закончилась память под ChainNode!");

        int allocatedNodeIndex = _firstFree;
        _firstFree = _components[allocatedNodeIndex].Next;

        // 3. Заполняем ноду данными компонента
        ref ChainNode node = ref _components[allocatedNodeIndex];
        node.Id = componentId;
        node.Generation = generation;
        node.Type = type;

        // 4. МГНОВЕННОЕ РАЗРЕШЕНИЕ ХОСТА: берем прямую ref-ссылку на живой DCHost из массива
        ref DCHost host = ref GlobalHosts[hostHandle.Id];

        // 5. ВШИВАЕМ НОДУ В НАЧАЛО ЦЕПОЧКИ ЖИВОГО ОБЪЕКТА
        node.Next = host.FirstComponent; 
        host.FirstComponent = allocatedNodeIndex; 
    }

    // Удаление ноды из цепочки за O(1) и возвращение во FreeList
    public void Remove(HostHandle hostHandle, int componentId, System.Type type) 
    {
        ref DCHost host = ref GlobalHosts[hostHandle.Id];
        
        int currentIndex = host.FirstComponent;
        int previousIndex = -1;

        while (currentIndex >= 0) 
        {
            ref ChainNode node = ref _components[currentIndex];

            if (node.Id == componentId && node.Type == type) 
            {
                if (previousIndex == -1)
                {
                    host.FirstComponent = node.Next;
                }
                else
                {
                    _components[previousIndex].Next = node.Next;
                }

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
    public bool Contains(HostHandle hostHandle, int componentId, System.Type type) 
    {
        int currentIndex = GlobalHosts[hostHandle.Id].FirstComponent;
        
        while (currentIndex >= 0) 
        {
            ref ChainNode node = ref _components[currentIndex];
            if (node.Id == componentId && node.Type == type) return true;
            currentIndex = node.Next;
        }
        return false;
    }

    public ChainNode GetTypedHandle(HostHandle hostHandle, System.Type type)
    {
        int currentIndex = GlobalHosts[hostHandle.Id].FirstComponent;

        while (currentIndex >= 0) 
        {
            ref ChainNode node = ref _components[currentIndex];
            if (node.Type == type) return node;
            currentIndex = node.Next;
        }
        return default;
    }
}