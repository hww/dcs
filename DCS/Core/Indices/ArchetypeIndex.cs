using System.Collections.Generic;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Derived index: tag bit -> list of hostIds.
    /// Rebuild is O(N) over ArchetypeComponent pool.
    /// </summary>
    public sealed class ArchetypeIndex
    {
        public struct Item
        {
            public Host Host;
            public int Next;
        }

        private readonly List<Item> _chain = new List<Item>(128);
        private readonly Dictionary<uint, ushort> _byName = new Dictionary<uint, ushort>(128);

        private int _firstFree;
        private int _firstUsed;

        public ArchetypeIndex()
        {
            _firstFree = -1;
            _firstUsed = -1;
        }

        public void Clear()
        {
            _byName.Clear();

            if (_chain.Count == 0)
            {
                _firstFree = -1;
                _firstUsed = -1;
                return;
            }

            for (int i = 0; i < _chain.Count; i++)
            {
                _chain[i] = new Item { Host = Host.Null, Next = i + 1 };
            }

            var last = _chain[_chain.Count - 1];
            last.Next = -1;
            _chain[_chain.Count - 1] = last;

            _firstFree = 0;
            _firstUsed = -1;
        }

        /// <summary>
        /// Добавляет Host в индекс по имени (tag bit).
        /// Если Host уже присутствует в цепочке — ничего не делает.
        /// </summary>
        public void Add(uint name, Host host)
        {
            // Проверяем, есть ли уже такая цепочка
            if (!_byName.TryGetValue(name, out ushort head))
            {
                // Цепочки нет — создаём новую
                head = (ushort)AllocateSlot();
                _chain[head] = new Item { Host = host, Next = -1 };
                _byName[name] = head;
                return;
            }

            // Проверяем дубликат
            if (Contains(head, host))
                return;

            // Вставляем в начало цепочки (O(1))
            int newIndex = AllocateSlot();
            _chain[newIndex] = new Item { Host = host, Next = head };
            _byName[name] = (ushort)newIndex;

            Debug.Log($"[ArchetypeIndex] Registered `{Names.ToString(name)}` Host {host.ToString()}");
        }

        /// <summary>
        /// Возвращает первый Host по заданному имени (tag bit), либо Host.Null.
        /// </summary>
        public Host GetFirstBy(uint name)
        {
            if (!_byName.TryGetValue(name, out ushort head))
                return Host.Null;

            return _chain[head].Host;
        }

        /// <summary>
        /// Возвращает все Host'ы по заданному имени (tag bit).
        /// Если нужно избежать аллокаций — передайте готовый список.
        /// </summary>
        public void GetAllBy(uint name, List<Host> result)
        {
            if (result == null)
                return;

            if (!_byName.TryGetValue(name, out ushort head))
                return;

            var current = (int)head;
            while (current >= 0)
            {
                var item = _chain[current];
                result.Add(item.Host);
                current = item.Next;
            }
        }

        /// <summary>
        /// Удобная перегрузка: возвращает новый список.
        /// </summary>
        public List<Host> GetAllBy(uint name)
        {
            var result = new List<Host>();
            GetAllBy(name, result);
            return result;
        }

        /// <summary>
        /// Удаляет Host из цепочки по имени.
        /// </summary>
        public bool Remove(uint name, Host host)
        {
            if (!_byName.TryGetValue(name, out ushort head))
                return false;

            int current = head;
            int prev = -1;

            while (current >= 0)
            {
                var item = _chain[current];
                if (item.Host == host)
                {
                    if (prev < 0)
                    {
                        // Удаляем голову
                        if (item.Next < 0)
                        {
                            _byName.Remove(name);
                            FreeSlot(current);
                        }
                        else
                        {
                            _byName[name] = (ushort)item.Next;
                            FreeSlot(current);
                        }
                    }
                    else
                    {
                        var prevItem = _chain[prev];
                        prevItem.Next = item.Next;
                        _chain[prev] = prevItem;
                        FreeSlot(current);
                    }
                    return true;
                }

                prev = current;
                current = item.Next;
            }

            return false;
        }

        private bool Contains(int first, Host host)
        {
            var current = first;

            while (current >= 0)
            {
                var item = _chain[current];
                if (item.Host == host)
                    return true;

                current = item.Next;
            }

            return false;
        }

        /// <summary>
        /// Выделяет свободный слот в _chain (переиспользует через free-list).
        /// </summary>
        private int AllocateSlot()
        {
            if (_firstFree >= 0)
            {
                int slot = _firstFree;
                _firstFree = _chain[slot].Next;
                _chain[slot] = new Item { Host = Host.Null, Next = _firstUsed };
                _firstUsed = slot;
                return slot;
            }

            // Свободных слотов нет — расширяем
            _chain.Add(new Item { Host = Host.Null, Next = _firstUsed });
            _firstUsed = _chain.Count - 1;
            return _firstUsed;
        }

        /// <summary>
        /// Возвращает слот в free-list.
        /// </summary>
        private void FreeSlot(int index)
        {
            _chain[index] = new Item { Host = Host.Null, Next = _firstFree };
            _firstFree = index;
        }
    }
}