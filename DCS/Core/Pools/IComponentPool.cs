using DCS.Spatial;
using System;
using System.Text;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Interface for all component pools, used for lifecycle management.
    /// </summary>
    /// <remarks>
    /// Provides a non-generic interface for operations that work with any pool:
    /// - SystemFree: Called by HostChainManager when a host is destroyed
    /// - ClearFramePool: Resets frame-based event pools
    /// - SystemDeliver: Delivers messages to receivers
    ///
    /// This interface enables type-safe access to pools without knowing T.
    /// </remarks>
    public interface IComponentPool : IFieldAccessForIndex
    {
        /// <summary>
        /// Allows non-generic allocation via the native Lua bridge
        /// </summary>
        /// <param name="hostHandle"></param>
        /// <param name="chain"></param>
        /// <returns></returns>
        Handle SystemAllocate(Host hostHandle, HostChain chain);

        Handle SystemAllocate(Host hostHandle, HostChain chain, object prius);

        /// <summary>
        /// Frees a component as part of host destruction.
        /// </summary>
        /// <param name="hostHandle">Host that owns the component.</param>
        /// <param name="chain">Host chain manager.</param>
        /// <param name="handle">Handle to the component to free.</param>
        void SystemFree(Host hostHandle, HostChain chain, Handle handle);

        /// <summary>
        /// Clears all frame-based components from the pool.
        /// </summary>
        void ClearFramePool();

        /// <summary>
        /// Delivers a message to a component at the specified roster index.
        /// </summary>
        /// <param name="rosterIndex">Index in the Roster.</param>
        /// <param name="msgTypeId">Type ID of the message.</param>
        /// <param name="msgHandle">Message handle with generation validation.</param>
        void SystemDeliver(int rosterIndex, int msgTypeId, Handle msgHandle);

        /// <summary>
        /// Tries to get the dense index from a Handle.
        /// Validates the Handle's Generation against the Roster slot.
        /// </summary>
        bool TryGetDenseIndex(Handle handle, out int denseIndex);

        /// <summary>
        /// Tries to get the Host that owns the component identified by the given Handle.
        /// Validates the Handle's Generation against the Roster slot.
        /// </summary>
        bool TryGetHost(Handle handle, out Host host);

        /// <summary>
        /// Assign pool id
        /// </summary>
        /// <param name="newId"></param>
        void SetPoolId(int newId);
    }

}