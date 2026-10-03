using DCS.Spatial;
using System;
using System.Text;
using UnityEngine;

namespace DCS.Core
{
    // ============================================================
    //  COMPONENT INTERFACES
    // ============================================================
    /// <summary>
    /// Base interface for all DCS components.
    /// Required for storage in ComponentManager and EventManager pools.
    /// </summary>
    public interface IComponent
    {
    }

    /// <summary>
    /// Base interface for all DCS components.
    /// Required for storage in ComponentManager and EventManager pools.
    /// </summary>
    /// <remarks>
    /// Every component must store its index in the Roster (RosterIndex).
    /// This is required for correct reference updates during Swap-Back
    /// when a component is removed from the dense pool.
    /// </remarks>
    public interface IHostable : IComponent
    {
        /// <summary>
        /// Component index in the Roster array.
        /// Set during allocation and updated when moved within the pool.
        /// </summary>
        Host Host { get; set; }
    }
}