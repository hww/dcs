using System;

namespace DCS.Core
{
    /// <summary>
    /// Safe reference to a Host (game object owner).
    /// Stored as 16-bit ID + 16-bit Generation. Packed as a 32-bit integer.
    /// Generation protects against stale references (Handle/Generation validation happens inside the Pool).
    /// </summary>
    [System.Serializable]
    public struct Host
    {
        public static readonly Host Null = new Host() { Id = HandleConfig.NULL_INDEX, Generation = 0 }; 
        public ushort Id;
        public ushort Generation;

        /// <summary>
        /// Checks if this Host is null (Id == NULL_INDEX).
        /// Generation is NOT checked for null because generation 0 is valid.
        /// </summary>
        public bool IsNull => Id == HandleConfig.NULL_INDEX;

        /// <summary>
        /// Pack Host into a single 32-bit integer for efficient passing to Lua.
        /// </summary>
        public int ToLua()
        {
            return (Id & HandleConfig.ID_MASK) | ((Generation & HandleConfig.GEN_MASK) << HandleConfig.GEN_SHIFT);
        }

        /// <summary>
        /// Unpack a 32-bit integer back into a Host struct.
        /// </summary>
        public static Host FromLua(int packed)
        {
            return new Host
            {
                Id = (ushort)(packed & HandleConfig.ID_MASK),
                Generation = (ushort)((packed >> HandleConfig.GEN_SHIFT) & HandleConfig.GEN_MASK)
            };
        }

        /// <summary>
        /// Constructor for creating a Host from explicit ID and Generation.
        /// </summary>
        public Host(ushort id, ushort generation)
        {
            Id = id;
            Generation = generation;
        }

        /// <summary>
        /// Constructor for unpacking from a packed int.
        /// </summary>
        public Host(int packed)
        {
            Id = (ushort)(packed & HandleConfig.ID_MASK);
            Generation = (ushort)((packed >> HandleConfig.GEN_SHIFT) & HandleConfig.GEN_MASK);
        }

        // Заглушка для компиляции, у вас она уже есть
        public static bool operator ==(Host a, Host b) => a.Id==b.Id && a.Generation==b.Generation; // Ваша логика сравнения
        public static bool operator !=(Host a, Host b) => a.Id != b.Id || a.Generation != b.Generation;

        public override string ToString() => $"Host(Id:{Id}, Gen:{Generation})";
    }
}