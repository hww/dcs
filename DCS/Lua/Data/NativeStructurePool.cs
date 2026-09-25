using System;
using System.Collections.Generic;
using UnityEngine;

namespace DCS.Data
{

    // Универсальный пул для нативных структур (Векторов, Кватернионов и т.д.)
    public class NativeStructurePool<T> where T : struct
    {
        private T[] _buffer;
        private int[] _refCounts;
        private Stack<int> _freeSlots;
        private int _maxCapacity;

        public NativeStructurePool(int initialCapacity = 2048)
        {
            _maxCapacity = initialCapacity;
            _buffer = new T[initialCapacity];
            _refCounts = new int[initialCapacity];
            _freeSlots = new Stack<int>(initialCapacity);

            // Заполняем стек свободных индексов с конца в начало
            for (int i = initialCapacity - 1; i >= 0; i--)
            {
                _freeSlots.Push(i);
            }
        }

        // Выделить новый слот под структуру
        public int Allocate(T value)
        {
            if (_freeSlots.Count == 0)
            {
                // Динамически расширяем пул, если память закончилась (как kheap/dead-pool в GOAL)
                ExpandPool();
            }

            int slot = _freeSlots.Pop();
            _buffer[slot] = value;
            _refCounts[slot] = 0; // Инициализируем без ссылок (LUA сделает Retain при оборачивании)
            return slot;
        }

        // Получить значение из слота по индексу
        public T Get(int index)
        {
            return _buffer[index];
        }

        // Записать новое значение в существующий слот
        public void Set(int index, T value)
        {
            _buffer[index] = value;
        }

        // Увеличить счетчик ссылок (Вызывается из LUA конструктора)
        public void Retain(int index)
        {
            _refCounts[index]++;
        }

        // Уменьшить счетчик ссылок (Вызывается из LUA __gc)
        public void Release(int index)
        {
            _refCounts[index]--;

            // Если на этот индекс больше никто не ссылается ни в LUA, ни в C# — освобождаем слот
            if (_refCounts[index] <= 0)
            {
                _buffer[index] = default(T); // Обнуляем память структуры
                _refCounts[index] = 0;
                _freeSlots.Push(index); // Возвращаем индекс в пул свободных слотов
            }
        }

        private void ExpandPool()
        {
            int oldCapacity = _maxCapacity;
            _maxCapacity *= 2;

            Array.Resize(ref _buffer, _maxCapacity);
            Array.Resize(ref _refCounts, _maxCapacity);

            for (int i = _maxCapacity - 1; i >= oldCapacity; i--)
            {
                _freeSlots.Push(i);
            }
            Debug.LogWarning($"[DCS Pool] {typeof(T).Name} pool expanded to {_maxCapacity} slots.");
        }
    }
}
