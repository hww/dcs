public class EventManager<T> : ComponentManager<T> where T : struct, IEventData
{
    public EventManager(int capacity) : base(capacity) { }

    // Метод Allocate для покадровых событий упрощен (им не нужен FreeList, они идут строго подряд)
    public ComponentHandle AllocateEvent(HostHandle host_handle, uint namespaceMask, ref Chain chain)
    {
        if (Partition >= Components.Length) throw new System.Exception("DCS Error: Превышена емкость пул-событий!");

        int denseIndex = Partition;
        Partition++;

        // Для ивентов Roster-индекс равен dense-индексу, так как дыр в памяти кадра нет!
        Roster[denseIndex] = denseIndex;
        Generations[denseIndex]++;
        int currentGen = Generations[denseIndex];

        Hosts[denseIndex] = host_handle;
        RosterIndices[denseIndex] = denseIndex;

        ref T ev = ref Components[denseIndex];
        ev = default;
        ev.NamespaceMask = namespaceMask; // Жестко пишем маску в структуру!

        // Вшиваем в Chain типа
        chain.Add(host_handle, denseIndex, ComponentType<T>.Id, currentGen);

        return new ComponentHandle { Id = denseIndex, Generation = currentGen };
    }
}