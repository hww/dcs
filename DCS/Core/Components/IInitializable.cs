using DCS.Spatial;
using System;
using System.Text;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Interface for components that support initialization via Prius.
    /// </summary>
    /// <remarks>
    /// Prius is an arbitrary object passed to Allocate().
    /// Used to pass initialization data (e.g., NavQuery, configs).
    /// </remarks>
    public interface IInitializable
    {
        /// <summary>
        /// Initializes the component with the provided data.
        /// </summary>
        /// <param name="prius">Initialization data object (can be null).</param>
        void Init(object prius);
    }
}