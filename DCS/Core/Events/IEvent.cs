using DCS.Spatial;
using System;
using System.Text;
using UnityEngine;

namespace DCS.Core
{
    // ============================================================
    //  EVENT INTERFACES
    // ============================================================

    /// <summary>
    /// Marker interface for event components.
    /// </summary>
    /// <remarks>
    /// Events are short-lived components (typically 1 frame).
    /// They are stored in separate pools (EventManager) and processed via EventSystem.
    /// Key feature: NamespaceMask for subscription filtering.
    /// </remarks>
    public interface IEvent : IHostable
    {
        /// <summary>
        /// Namespace mask for subscription filtering.
        /// </summary>
        /// <remarks>
        /// Used in EventSystem.PollEvents for checking:
        /// (ev.NamespaceMask & sub.NamespaceMask) != 0
        /// </remarks>
        uint NamespaceMask { get; set; }
    }
}