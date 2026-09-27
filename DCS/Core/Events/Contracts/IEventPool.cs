namespace DCS.Core
{
    /// <summary>
    /// Не-generic интерфейс event-пула.
    /// Позволяет EventSystem работать с пулами без знания T.
    /// </summary>
    public interface IEventPool : IComponentPool
    {
        int EventPartition { get; }

        void GetMessageHandle(int denseIndex, out Handle handle);
        uint GetMessageNamespaceMask(int denseIndex);
        Host GetSenderHost(int denseIndex);
    }
}