using DCS.Spatial;
using System;
using System.Text;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Interface for dispatching events from event pools.
    /// </summary>
    /// <remarks>
    /// Implemented by event managers (EventManager{T}) for integration with the delivery system.
    /// Called from EventSystem.UpdateComponents during the Update phase.
    ///
    /// Lifecycle:
    /// 1. SystemPoll() collects all active events from the pool
    /// 2. Matches them with subscriptions via TypeChainManager
    /// 3. Builds an invocation list (InvokeList)
    /// 4. EventSystem.DeliverEvents() delivers them to receivers
    /// </remarks>
    public interface IEventDispatcher
    {
        /// <summary>
        /// Polls the event pool to collect invocations.
        /// </summary>
        /// <param name="subManager">Subscription manager (contains SubscriptionNode).</param>
        /// <param name="typeChain">Type chain manager (links events to subscriptions).</param>
        /// <remarks>
        /// Called by the system during the Update phase.
        /// The method should:
        /// - Iterate all active events in the pool
        /// - Find subscriptions via typeChain.GetTypeChainHead(eventTypeId)
        /// - Check mask matches (ev.NamespaceMask & sub.NamespaceMask)
        /// - Add records to EventSystem._invokeList for later delivery
        /// </remarks>
        void SystemPoll(EventSubscription subManager, TypeChain typeChain);
    }
}