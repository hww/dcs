using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

// Интерфейс для базового пула (управление жизненным циклом из Chain)
public interface IComponentPool
{
    void SystemFree(HostHandle hostHandle, HostChainManager chain, int rosterId);
    void ClearFramePool();
    void SystemDeliver(int denseIndex, MessageHandle msgHandle); // <- Добавить
}

public interface IDcsComponent
{
    int RosterIndex { get; set; }
}

public struct RosterItem 
{
    public int Index;          // DenseIndex (указатель на плотный массив компонентов)
    public int Generation;     // Поколение для валидации хэндла
    public HostHandle Host;    // Владелец компонента (нужен для вызова chain.Remove при удалении)

    public int Next;           // Следующая ячейка (свободная или занятая)
}
public class ComponentManager<T> : IComponentPool where T : struct 
{
    public int Partition = 0; 
    public T[] Components;       
    public RosterItem[] Roster; // Лаконичный холодный контур (Связи, поколения)

    protected int _freeRosterHead = -1; 
    protected int _rosterIncr = 0;       
    private readonly System.Type _componentType;
    private readonly int _poolId;      
    private EUpdateStage _updateStages;
    private EAsyncUpdateStage _asyncUpdateStages;
    private uint _mask;

    public ComponentManager(int capacity, EUpdateStage updateStages = EUpdateStage.Update, EAsyncUpdateStage asyncUpdateStages = EAsyncUpdateStage.None, uint mask = 0)
    {
        _componentType = typeof(T);
        _poolId = ComponentType<T>.Id; 
        _updateStages = updateStages;
        _asyncUpdateStages = asyncUpdateStages;
        _mask = mask;
        
        Components = new T[capacity];
        // ИСПРАВЛЕНО: Выделяем память под правильный тип структуры RosterItem
        Roster = new RosterItem[capacity]; 
        
        // Организация списка свободных слотов ростера (Free List) через поле Next
        for (int i = 0; i < capacity; i++) 
            Roster[i].Next = i + 1;
            
        Roster[capacity - 1].Next = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T ResolveHandle(ComponentHandle component_handle)
    {
        int rosterIndex = component_handle.Id;
        // Проверяем поколение прямо в структуре ростера
        if (Roster[rosterIndex].Generation == component_handle.Generation)
        {
            int denseIndex = Roster[rosterIndex].Index;
            return ref Components[denseIndex];
        }
        throw new System.InvalidCastException($"DCS ValidCast Error: Хэндл устарел для пула {_componentType.Name}");
    }

    public ComponentHandle Allocate(HostHandle host_handle, HostChainManager chain) => Allocate(host_handle, chain, null);

       public ComponentHandle Allocate(HostHandle host_handle, HostChainManager chain, object prius) 
    {
        if (Partition >= Components.Length)
            throw new System.Exception($"DCS Error: Превышена емкость пула для {_componentType.Name}!");

        int rosterIndex = (_freeRosterHead != -1) ? _freeRosterHead : _rosterIncr++;
        if (_freeRosterHead != -1) 
            _freeRosterHead = Roster[_freeRosterHead].Next; 

        int denseIndex = Partition++;
        
        Roster[rosterIndex].Index = denseIndex;
        Roster[rosterIndex].Generation++; 
        Roster[rosterIndex].Host = host_handle; 
        
        int currentGen = Roster[rosterIndex].Generation;

        ref T comp = ref Components[denseIndex];
        comp = default;

        if (comp is IDcsComponent dcsComp)
        {
            dcsComp.RosterIndex = rosterIndex;
        }

        if (prius != null && comp is IDcsInitializable initializable) 
            initializable.Init(prius);

        // ИСПРАВЛЕНО: Собираем хэндл и передаем в типизированный Add
        ComponentHandle handle = new ComponentHandle { Id = rosterIndex, Generation = currentGen };
        chain.Add(host_handle, handle, _poolId);
        
        return handle;
    }

    public void Free(HostHandle host_handle, HostChainManager chain, ref ComponentHandle component_handle) 
    {
        int rosterIndexToDelete = component_handle.Id; 
        int denseIndexToDelete = Roster[rosterIndexToDelete].Index;

        // ИСПРАВЛЕНО: Передаем хэндл целиком вместо сырого int индекса
        chain.Remove(Roster[rosterIndexToDelete].Host, component_handle, _poolId);

        Roster[rosterIndexToDelete].Generation++;
        Roster[rosterIndexToDelete].Next = _freeRosterHead; 
        _freeRosterHead = rosterIndexToDelete;              

        Partition--;
        int denseIndexToMove = Partition; 

        if (denseIndexToDelete != denseIndexToMove)
        {
            Components[denseIndexToDelete] = Components[denseIndexToMove];

            if (Components[denseIndexToDelete] is IDcsComponent movingComp)
            {
                int movingRosterIndex = movingComp.RosterIndex;
                Roster[movingRosterIndex].Index = denseIndexToDelete;
            }
        }
        component_handle = default;
    }


    public virtual void ClearFramePool()
    {
        System.Array.Clear(Components, 0, Partition);
        Partition = 0;
        _rosterIncr = 0;
        _freeRosterHead = -1;
    }

    public void SystemDeliver(int denseIndex, MessageHandle msgHandle)
    {
        if (Components[denseIndex] is IDcsMessageReceiver receiver)
        {
            receiver.ReceiveMessage(msgHandle);
        }
    }

    // Системный мост для HostChainManager.FreeChain
    void IComponentPool.SystemFree(HostHandle hostHandle, HostChainManager chain, int rosterId)
    {
        // ИСПРАВЛЕНО: Извлекаем поколение из структуры RosterItem под типом HostChainManager
        ComponentHandle handle = new ComponentHandle { Id = rosterId, Generation = Roster[rosterId].Generation };
        Free(hostHandle, chain, ref handle);
    }
}