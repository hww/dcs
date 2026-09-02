using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

// Интерфейс для базового пула (управление жизненным циклом из Chain)
public interface IComponentPool
{
    void SystemFree(HostHandle hostHandle, Chain chain, int rosterId);
    void ClearFramePool();
}

public class ComponentManager<T> : IComponentPool where T : struct 
{
    public int Partition = 0; 
    public T[] Components;       
    public int[] Roster;         
    public int[] Generations;    
    public HostHandle[] Hosts;   
    public int[] RosterIndices;  

    protected int _freeRosterHead = -1; 
    protected int _rosterIncr = 0;       
    private readonly System.Type _componentType;
    private readonly int _poolId;      
    private EUpdateStage _updateStages;
    private EUpdateStage _asyncUpdateStages;
    private uint _mask;

    public ComponentManager(int capacity, EUpdateStage updateStages = EUpdateStage.Update, EUpdateStage asyncUpdateStages = EUpdateStage.None, uint mask = 0)
    {
        _componentType = typeof(T);
        _poolId = ComponentType<T>.Id; 
        _updateStages = updateStages;
        _asyncUpdateStages = asyncUpdateStages;
        _mask = mask;
        Components = new T[capacity];
        Hosts = new HostHandle[capacity];
        Roster = new int[capacity];
        Generations = new int[capacity];
        RosterIndices = new int[capacity];
        
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
        throw new System.InvalidCastException($"DCS ValidCast Error: Хэндл устарел для пула {_componentType.Name}");
    }

    public ComponentHandle Allocate(HostHandle host_handle, Chain chain) => Allocate(host_handle, chain, null);

    public ComponentHandle Allocate(HostHandle host_handle, Chain chain, object prius) 
    {
        if (Partition >= Components.Length)
            throw new System.Exception($"DCS Error: Превышена емкость пула для {_componentType.Name}!");

        int rosterIndex = (_freeRosterHead != -1) ? _freeRosterHead : _rosterIncr++;
        if (_freeRosterHead != -1) _freeRosterHead = ~Roster[rosterIndex]; 

        int denseIndex = Partition++;
        Roster[rosterIndex] = denseIndex;
        Generations[rosterIndex]++; 
        int currentGen = Generations[rosterIndex];

        Hosts[denseIndex] = host_handle;
        RosterIndices[denseIndex] = rosterIndex;
        
        ref T comp = ref Components[denseIndex];
        comp = default;

        if (prius != null && comp is IDcsInitializable initializable) initializable.Init(prius);

        chain.Add(host_handle, rosterIndex, _poolId, currentGen);
        return new ComponentHandle { Id = rosterIndex, Generation = currentGen };
    }

    // Системный мост для Chain.FreeChain
    void IComponentPool.SystemFree(HostHandle hostHandle, Chain chain, int rosterId)
    {
        ComponentHandle handle = new ComponentHandle { Id = rosterId, Generation = Generations[rosterId] };
        Free(hostHandle, chain, ref handle);
    }

    public void Free(HostHandle host_handle, Chain chain, ref ComponentHandle component_handle) 
    {
        int rosterIndexToDelete = component_handle.Id; 
        int denseIndexToDelete = Roster[rosterIndexToDelete];

        chain.Remove(host_handle, rosterIndexToDelete, _poolId);

        Generations[rosterIndexToDelete]++;
        Roster[rosterIndexToDelete] = ~_freeRosterHead;
        _freeRosterHead = rosterIndexToDelete;

        Partition--;
        int denseIndexToMove = Partition; 

        if (denseIndexToDelete != denseIndexToMove)
        {
            Components[denseIndexToDelete] = Components[denseIndexToMove];
            Hosts[denseIndexToDelete] = Hosts[denseIndexToMove];
            RosterIndices[denseIndexToDelete] = RosterIndices[denseIndexToMove];

            int movingRosterIndex = RosterIndices[denseIndexToDelete];
            Roster[movingRosterIndex] = denseIndexToDelete;
        }
        component_handle = default;
    }

    public virtual void ClearFramePool()
    {
        Partition = 0;
        _rosterIncr = 0;
        _freeRosterHead = -1;
    }
}