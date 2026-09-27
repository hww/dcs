using DCS.Spatial;
using System;
using System.Text;
using UnityEngine;

namespace DCS.Core
{
    public interface IFieldAccessForIndex
    {
        /// <summary>
        /// Reads a field from a component and pushes it to Lua stack.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        bool GetField(int denseIndex, string fieldName, IntPtr L);

        /// <summary>
        /// Reads a value from Lua stack and writes it to a component field.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        bool SetField(int denseIndex, string fieldName, IntPtr L);
    }

    public interface IFieldAccess
    {
        /// <summary>
        /// Reads a field from a component and pushes it to Lua stack.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        bool GetField(string fieldName, IntPtr L);

        /// <summary>
        /// Reads a value from Lua stack and writes it to a component field.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        bool SetField(string fieldName, IntPtr L);

    }
}