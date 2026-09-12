using System;
using UnityEngine;

namespace DynamicComponent
{
    [RequireComponent(typeof(Camera))]
    public class BaseCamera : BaseActor
    {
        public Camera UnityCamera { get; private set; }

        protected virtual void Awake()
        {
            UnityCamera = GetComponent<Camera>();
        }

        // ================================================
        // IFieldAccess
        // ================================================

        // Универсальный интерфейс, который будет вызывать Lua через C# Reflection или обертку
        /// <summary>
        /// Reads a field from a component and pushes it to Lua stack.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        public override bool GetField(string fieldName, IntPtr L) { return false; }

        /// <summary>
        /// Reads a value from Lua stack and writes it to a component field.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        public override bool SetField(string fieldName, IntPtr L) { return false; }
    }
}
