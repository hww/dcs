using System;
using System.Runtime.CompilerServices;


namespace DCS.Core
{
    /// <summary>
    /// Делегат доставки события.
    /// Вызывается для каждой пары (сообщение × подписка),
    /// где совпал namespace mask.
    /// </summary>
    public delegate void MessageHandler(
        in SubscriptionNode subscription,
        Host senderHost,
        int eventTypeId,
        Handle messageHandle);

    public static class EventSystem
    {
        /// <summary>
        /// Доставить все события за кадр.
        /// Игра передаёт делегат — что именно делать:
        /// - вызвать Lua (LuaManager.CallEventRouter),
        /// - вызвать C#-получателя (pool.SystemDeliver),
        /// - или и то, и другое.
        ///
        /// Обход:
        ///   все event-пулы
        ///   → все сообщения в пуле
        ///   → все подписки на этот тип
        ///   → если (message.Mask &amp; sub.Mask) != 0 → handler(...)
        /// </summary>
        public static void DeliverAll(Domain domain, MessageHandler handler)
        {
            if (domain == null || handler == null) return;

            var subscriptionPool = domain.SubscriptionPool;
            var typeChain = domain.TypeChain;

            for (int typeIdx = 0; typeIdx < ComponentRegistry.PollTypesCount; typeIdx++)
            {
                int eventTypeId = ComponentRegistry.PollTypeIds[typeIdx];
                var pool = ComponentRegistry.Pools[eventTypeId];
                if (pool is not IEventPool eventPool) continue;

                int partition = eventPool.EventPartition;
                if (partition == 0) continue;

                int nodeIdx = typeChain.GetTypeChainHead(eventTypeId);
                while (nodeIdx >= 0)
                {
                    ref var chainNode = ref typeChain.GetNode(nodeIdx);
                    ref var sub = ref subscriptionPool.ResolveHandle(chainNode.SubscriptionHandle);

                    for (int msgIdx = 0; msgIdx < partition; msgIdx++)
                    {
                        uint msgMask = eventPool.GetMessageNamespaceMask(msgIdx);
                        if ((msgMask & sub.NamespaceMask) == 0) continue;

                        eventPool.GetMessageHandle(msgIdx, out Handle messageHandle);
                        Host sender = eventPool.GetSenderHost(msgIdx);

                        handler(in sub, sender, eventTypeId, messageHandle);
                    }

                    nodeIdx = chainNode.Next;
                }
            }
        }
    }
}

