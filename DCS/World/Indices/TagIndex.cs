using System.Collections.Generic;

namespace DCS.Core
{
    /// <summary>
    /// Derived index: tag bit -> list of hostIds.
    /// Rebuild is O(N) over TagComponent pool.
    /// </summary>
    public sealed class TagIndex
    {
        // 32 buckets, one per bit.
        private readonly List<ushort>[] _byBit = new List<ushort>[32];

        public TagIndex()
        {
            for (int i = 0; i < 32; i++) _byBit[i] = new List<ushort>(64);
        }

        public void Add(ushort hostId, uint mask)
        {
            for (int bit = 0; bit < 32; bit++)
            {
                if ((mask & (1u << bit)) != 0)
                    _byBit[bit].Add(hostId);
            }
        }

        public void Remove(ushort hostId, uint mask)
        {
            for (int bit = 0; bit < 32; bit++)
            {
                if ((mask & (1u << bit)) != 0)
                    _byBit[bit].Remove(hostId);
            }
        }

        public void GetByTag(int bit, List<ushort> results)
        {
            results.Clear();
            if (bit < 0 || bit >= 32) return;
            results.AddRange(_byBit[bit]);
        }

        public void Clear()
        {
            for (int i = 0; i < 32; i++) _byBit[i].Clear();
        }
    }
}