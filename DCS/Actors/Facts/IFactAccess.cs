using DCS.Spatial;
using System;
using System.Text;
using UnityEngine;

namespace DCS.Actors
{
    public interface IFactAccess
    {
        /// <summary>
        /// Reads a field from a component and pushes it to Lua stack.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        bool GetFact(string fieldName, IntPtr L);

        /// <summary>
        /// Reads a value from Lua stack and writes it to a component field.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        bool SetFact(string fieldName, IntPtr L);

    }
}