namespace DCS.Core
{
    /// <summary>
    /// Домен — изолированный ECS-контекст.
    ///
    /// Один Domain = один HostChain + TypeChain + SubscriptionPool + Scheduler.
    /// Все хосты, компоненты, подписки и события живут внутри одного Domain.
    ///
    /// Domain НЕ знает про spatial. Связь с SpatialDomain — через SpatialDomainId (int).
    /// Один SpatialDomain может быть привязан к нескольким Domain (n:m).
    /// Domain может вообще не иметь spatial (SpatialDomainId == NoSpatial).
    /// </summary>
    public sealed class Domain
    {
        public const int DefaultId = 0;
        public const int NoSpatial = -1;

        // --- Identity ---
        public int Id { get; }
        public Name Name { get; }

        // --- ECS ---
        public HostChain HostChain { get; }
        public TypeChain TypeChain { get; }
        public EventSubscription SubscriptionPool { get; }
        public UpdateScheduler Scheduler { get; }

        // --- Spatial (ссылка, не объект) ---
        public int SpatialDomainId { get; private set; } = NoSpatial;
        public bool HasSpatial => SpatialDomainId != NoSpatial;

        public Domain(int id, string name, int hostCapacity, int subCapacity)
        {
            Id = id;
            Name = new Name(name);
            HostChain = new HostChain();
            TypeChain = new TypeChain();
            SubscriptionPool = new EventSubscription(subCapacity);
            Scheduler = new UpdateScheduler();
        }

        /// <summary>
        /// Привязывает SpatialDomain по id.
        /// Один SpatialDomain может быть привязан к нескольким Domain.
        /// </summary>
        public void AttachSpatial(int spatialDomainId)
        {
            SpatialDomainId = spatialDomainId;
        }

        public void DetachSpatial()
        {
            SpatialDomainId = NoSpatial;
        }
    }
}