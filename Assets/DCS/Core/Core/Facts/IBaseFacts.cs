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
}