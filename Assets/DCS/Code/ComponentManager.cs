using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

public class ComponentManager<T> where T : struct 
{
    public int Partition = 0; 
    
    public T[] Components;       
    public int[] Roster;         
    public int[] Generations;    
    public HostHandle[] Hosts;   

    private int _freeRosterHead = -1; 
    private int _rosterIncr = 0;       
    private readonly System.Type _componentType;
    private readonly int _poolId;      

    public ComponentManager(int capacity)
    {
        _componentType = typeof(T);
        _poolId = ComponentType<T>.Id; 

        Components = new T[capacity];
        Hosts = new HostHandle[capacity];
        Roster = new int[capacity];
        Generations = new int[capacity];
        
        for (int i = 0; i < capacity; i++) Roster[i] = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T ResolveHandle(ComponentHandle component_handle)
    {
        int rosterIndex = component_handle.Id;

        if (Generations[rosterIndex] == component_handle.Generation)
        {
            int denseIndex = Roster[rosterIndex];
         return ref Components[denseIndex];
    }

        throw new System.InvalidCastException($"DCS ValidCast Error: Хэндл устарел или недействителен для пула {_componentType.Name}");
    }

    public ComponentHandle Allocate(HostHandle host_handle, ref Chain chain, object prius = null) 
    {
        if (Partition >= Components.Length)
        {
            throw new System.Exception($"DCS Error: Превышена емкость пула для {_componentType.Name}!");
        }

        int rosterIndex;
        if (_freeRosterHead != -1)
        {
            rosterIndex = _freeRosterHead;
            _freeRosterHead = ~Roster[rosterIndex]; 
        }
        else
        {
            rosterIndex = _rosterIncr++;
        }

        int denseIndex = Partition;
        Partition++;

        Roster[rosterIndex] = denseIndex;
        Generations[rosterIndex]++; 
        int currentGen = Generations[rosterIndex];

        Hosts[denseIndex] = host_handle;
        
        ref T comp = ref Components[denseIndex];
        comp = default;

        if (prius is float dmgAmount && typeof(T) == typeof(DamageEvent))
        {
            ref DamageEvent dmg = ref Unsafe.As<T, DamageEvent>(ref comp);
            dmg.Amount = dmgAmount;
        }
        else if (prius is int zoneId && typeof(T) == typeof(LocationEvent))
        {
            ref LocationEvent loc = ref Unsafe.As<T, LocationEvent>(ref comp);
            loc.ZoneId = zoneId;
        }
        else if (prius is float startSpeed && typeof(T) == typeof(RotatorComponent))
        {
            ref RotatorComponent rotator = ref System.Runtime.CompilerServices.Unsafe.As<T, RotatorComponent>(ref comp);
            rotator.Speed = startSpeed;
            rotator.CurrentAngle = UnityEngine.Random.Range(0f, 360f);
        }

        // ИСПРАВЛЕНИЕ СИГНАТУРЫ: Передаем параметры раздельно в новую индексную таблицу Chain
        chain.Add(host_handle, rosterIndex, _componentType, currentGen);

        return new ComponentHandle { Id = rosterIndex, Generation = currentGen };
    }

    public void Free(HostHandle host_handle, ref Chain chain, ref ComponentHandle component_handle) 
    {
        int rosterIndexToDelete = component_handle.Id; 
        int denseIndexToDelete = Roster[rosterIndexToDelete];

        // ИСПРАВЛЕНИЕ СИГНАТУРЫ: Удаляем связь из глобального реестра Chain по точным параметрам
        chain.Remove(host_handle, rosterIndexToDelete, _componentType);

        Generations[rosterIndexToDelete]++;
        Roster[rosterIndexToDelete] = ~_freeRosterHead;
        _freeRosterHead = rosterIndexToDelete;

        Partition--;
        int denseIndexToMove = Partition; 

        if (denseIndexToDelete != denseIndexToMove)
        {
            Components[denseIndexToDelete] = Components[denseIndexToMove];
            Hosts[denseIndexToDelete] = Hosts[denseIndexToMove];

            HostHandle movingElementHost = Hosts[denseIndexToDelete];

            // Получаем стабильную ноду связи из индексного списка
            ChainNode movingTypedHandle = chain.GetTypedHandle(movingElementHost, _componentType);
            Roster[movingTypedHandle.Id] = denseIndexToDelete;
        }
        component_handle = default;
    }

    public void ClearFramePool()
    {
        Partition = 0;
        _rosterIncr = 0;
        _freeRosterHead = -1;
    }
}

