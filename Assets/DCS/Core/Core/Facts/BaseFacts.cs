using System;
using System.Linq;
using UnityEngine;

namespace DynamicComponent
{ 
    /// <summary>
    /// Базовый интерфейс для системы фактов предоставляющей доступ к данным различных типов.
    /// Реализация должна обеспечивать эффективный доступ к данным через строковые идентификаторы.
    /// </summary>
    public interface IBaseFacts
    {
        /// <summary>
        /// Получает значение факта типа T. Выбрасывает исключение если факт не найден.
        /// </summary>
        /// <typeparam name="T">Тип значения (bool, int, float, string, Vector2, Vector3, Color)</typeparam>
        /// <param name="name">Идентификатор факта</param>
        /// <returns>Значение факта</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">Если факт не найден</exception>
        T Get<T>(string name);

        /// <summary>
        /// Получает значение факта или значение по умолчанию если факт не найден.
        /// </summary>
        /// <typeparam name="T">Тип значения</typeparam>
        /// <param name="name">Идентификатор факта</param>
        /// <param name="defaultValue">Значение возвращаемое если факт не найден</param>
        /// <returns>Значение факта или defaultValue</returns>
        T Get<T>(string name, T defaultValue);

        /// <summary>
        /// Пытается получить значение факта.
        /// </summary>
        /// <typeparam name="T">Тип значения</typeparam>
        /// <param name="name">Идентификатор факта</param>
        /// <param name="value">Найденное значение</param>
        /// <returns>True если факт найден, иначе False</returns>
        bool TryGet<T>(string name, out T value);

        /// <summary>
        /// Устанавливает или обновляет значение факта.
        /// </summary>
        /// <typeparam name="T">Тип значения</typeparam>
        /// <param name="name">Идентификатор факта</param>
        /// <param name="value">Значение факта</param>
        void Set<T>(string name, T value);

        /// <summary>
        /// Удаляет факт если он существует.
        /// </summary>
        /// <param name="name">Идентификатор факта</param>
        /// <returns>True если факт был удален, иначе False</returns>
        bool Remove(string name);

        /// <summary>
        /// Проверяет существует ли факт с указанным именем.
        /// </summary>
        /// <param name="name">Идентификатор факта</param>
        /// <returns>True если факт существует</returns>
        bool Contains(string name);

        /// <summary>
        /// Очищает все факты.
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Абстрактная базовая реализация IBaseFacts с общей логикой и валидацией.
    /// Наследники должны реализовать конкретное хранилище данных.
    /// </summary>
    public abstract class BaseFacts : IBaseFacts
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
    }
}