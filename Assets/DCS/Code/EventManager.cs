public class EventManager<T> : ComponentManager<T> where T : struct, IEventData
{
    // Передаем дефолтные параметры в обновленный конструктор базового класса
    public EventManager(int capacity) : base(capacity, EUpdateStage.Update, EAsyncUpdateStage.None, 0) { }

    // Метод Allocate для покадровых событий (идут строго подряд без дыр)
    public ComponentHandle AllocateEvent(HostHandle host_handle, uint namespaceMask, HostChainManager chain)
    {
        if (Partition >= Components.Length) 
            throw new System.Exception("DCS Error: Превышена емкость пул-событий!");

        int denseIndex = Partition;
        int rosterIndex = denseIndex; 
        Partition++;

        Roster[rosterIndex].Index = denseIndex;
        Roster[rosterIndex].Generation++;
        Roster[rosterIndex].Host = host_handle; 
        
        int currentGen = Roster[rosterIndex].Generation;

        ref T ev = ref Components[denseIndex];
        ev = default;
        ev.NamespaceMask = namespaceMask; 

        if (ev is IDcsComponent dcsComp)
        {
            dcsComp.RosterIndex = rosterIndex;
        }

        // ИСПРАВЛЕНО: Упаковка в хэндл и вызов перегруженного метода Add с 3 аргументами
        ComponentHandle handle = new ComponentHandle { Id = rosterIndex, Generation = currentGen };
        chain.Add(host_handle, handle, ComponentType<T>.Id);

        return handle;
    }
}
