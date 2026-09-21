namespace DCS.Core
{
    /// <summary>
    /// Домен — изолированный мир DCS. Один Domain = один HostChain + TypeChain + SubscriptionPool.
    /// Все компоненты, хосты, подписки и события живут внутри одного Domain и не пересекаются с другими.
    /// </summary>
    public sealed class Domain
    {
        public int Id { get; }
        public string Name { get; }

        public HostChain HostChain { get; }
        public TypeChain TypeChain { get; }
        public EventSubscription SubscriptionPool { get; }

        public Domain(int id, string name, int hostCapacity, int subCapacity)
        {
            Id = id;
            Name = name;
            HostChain = new HostChain();
            TypeChain = new TypeChain();
            SubscriptionPool = new EventSubscription(subCapacity);
        }
    }
}