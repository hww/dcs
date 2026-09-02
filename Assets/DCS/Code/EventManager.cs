public class EventManager<T> : ComponentManager<T> where T : struct, IEventData
{
    // Передаем дефолтные параметры в обновленный конструктор базового класса
    public EventManager(int capacity) : base(capacity, EUpdateStage.Update, EAsyncUpdateStage.None, 0) { }

    // Метод Allocate для покадровых событий (идут строго подряд без дыр)
    public ComponentHandle AllocateEvent(HostHandle host_handle, uint namespaceMask, Chain chain)
    {
        if (Partition >= Components.Length) 
            throw new System.Exception("DCS Error: Превышена емкость пул-событий!");

        // Для ивентов Roster-индекс равен dense-индексу, так как дыр в памяти кадра нет
        int denseIndex = Partition;
        int rosterIndex = denseIndex; 
        Partition++;

        // Заполняем паспорт элемента в структуре RosterItem
        Roster[rosterIndex].Index = denseIndex;
        Roster[rosterIndex].Generation++;
        Roster[rosterIndex].Host = host_handle; // Пишем хост в ростер
        
        int currentGen = Roster[rosterIndex].Generation;

        ref T ev = ref Components[denseIndex];
        ev = default;
        ev.NamespaceMask = namespaceMask; // Жестко пишем маску в структуру

        // Компонент-событие обязан знать свой RosterIndex для общей консистентности системы
        if (ev is IDcsComponent dcsComp)
        {
            dcsComp.RosterIndex = rosterIndex;
        }

        // Вшиваем связь в Chain, используя сгенерированный PoolId (Id типа)
        chain.Add(host_handle, rosterIndex, ComponentType<T>.Id, currentGen);

        return new ComponentHandle { Id = rosterIndex, Generation = currentGen };
    }
}
