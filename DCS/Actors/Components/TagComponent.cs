using System;

namespace DCS.Core
{
    /// <summary>
    /// Bitmask of tags. 32 tags max is usually enough; if you need more,
    /// use a second TagComponent type or a fixed-size uint[] field.
    /// Tags are represented as bit indices (0..31).
    /// </summary>
    [ComponentPool(4096)]
    public struct TagComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public uint Mask;
    }
}