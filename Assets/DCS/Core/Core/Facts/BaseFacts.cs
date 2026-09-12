using System;
using System.Linq;
using UnityEngine;

namespace DynamicComponent
{ 

    /// <summary>
    /// Абстрактная базовая реализация IBaseFacts с общей логикой и валидацией.
    /// Наследники должны реализовать конкретное хранилище данных.
    /// </summary>
    public abstract class BaseFacts : IBaseFacts, IFactAccess
    {
        /// <summary>
        /// Поддерживаемые типы данных для фактов.
        /// </summary>
        protected static readonly Type[] SupportedTypes =
        {
            typeof(bool), typeof(int), typeof(float), typeof(string),
            typeof(Vector2), typeof(Vector3), typeof(Color)
        };

        /// <summary>
        /// Проверяет поддерживается ли указанный тип данных.
        /// </summary>
        protected virtual bool IsTypeSupported<T>()
        {
            return Array.Exists(SupportedTypes, t => t == typeof(T));
        }

        /// <summary>
        /// Валидирует параметры перед операциями с фактами.
        /// </summary>
        protected virtual void ValidateParameters<T>(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Fact name cannot be null or empty", nameof(name));

            if (!IsTypeSupported<T>())
                throw new NotSupportedException($"Type {typeof(T).Name} is not supported. " +
                                              $"Supported types: {string.Join(", ", SupportedTypes.Select(t => t.Name))}");
        }

        // Абстрактные методы которые должны быть реализованы в наследниках
        protected abstract bool TryGetInternal<T>(string name, out T value);
        protected abstract void SetInternal<T>(string name, T value);
        protected abstract bool RemoveInternal(string name);
        protected abstract bool ContainsInternal(string name);
        protected abstract void ClearInternal();



        #region IBaseFacts Implementation

        public T Get<T>(string name)
        {
            ValidateParameters<T>(name);

            if (TryGetInternal<T>(name, out T value))
                return value;

            throw new System.Collections.Generic.KeyNotFoundException(
                $"Fact '{name}' of type {typeof(T).Name} not found");
        }

        public T Get<T>(string name, T defaultValue)
        {
            ValidateParameters<T>(name);
            return TryGetInternal<T>(name, out T value) ? value : defaultValue;
        }

        public bool TryGet<T>(string name, out T value)
        {
            ValidateParameters<T>(name);
            return TryGetInternal<T>(name, out value);
        }

        public void Set<T>(string name, T value)
        {
            ValidateParameters<T>(name);
            SetInternal(name, value);
        }

        public bool Remove(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Fact name cannot be null or empty", nameof(name));

            return RemoveInternal(name);
        }

        public bool Contains(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Fact name cannot be null or empty", nameof(name));

            return ContainsInternal(name);
        }

        public void Clear()
        {
            ClearInternal();
        }

        #endregion

        #region IFieldAccess
        // Универсальный интерфейс, который будет вызывать Lua через C# Reflection или обертку
        /// <summary>
        /// Reads a field from a component and pushes it to Lua stack.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        public abstract bool GetFact(string fieldName, IntPtr L);

        /// <summary>
        /// Reads a value from Lua stack and writes it to a component field.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        public abstract bool SetFact(string fieldName, IntPtr L);
        #endregion
    }
}