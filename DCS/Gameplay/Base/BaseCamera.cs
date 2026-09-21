using System;
using UnityEngine;

namespace DCS.Core
{
    [RequireComponent(typeof(CameraActor))]
    public class BaseCamera : BaseActor
    {
        public CameraActor UnityCamera { get; private set; }

        protected virtual void Awake()
        {
            UnityCamera = GetComponent<CameraActor>();
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
