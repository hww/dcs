using System;

namespace DCS.Core
{
    /// <summary>
    /// Safe reference to a component in a pool.
    /// </summary>
    /// <remarks>
    /// The handle contains an index into the Roster and a Generation.
    /// Generation is incremented each time a slot is freed, making old handles invalid.
    /// 
    /// Important: NULL_INDEX (65535) is reserved for "no slot".
    /// Id can be 0..65534 for valid slots.
    /// 
    /// The ONLY way to validate a handle is through the pool (Roster[Id].Generation == Generation).
    /// We do NOT provide IsNull() here because:
    /// 1. Generation can be 0 for a valid object (after 65,535 recycles).
    /// 2. Id = 0 is a valid index.
    /// 3. Checking IsNull on Id alone is not enough — need pool validation.
    /// </remarks>
    public struct Handle
    {
        /// <summary>Index into the Roster array (0..65534). NULL_INDEX (65535) means null.</summary>
        public ushort Id;

        /// <summary>Roster slot generation. Incremented on each free (0..65535).</summary>
        public ushort Generation;

        /// <summary>
        /// Static null handle instance. Id = NULL_INDEX, Generation = 0.
        /// </summary>
        public static readonly Handle Null = new Handle
        {
            Id = (ushort)HandleConfig.NULL_INDEX,
            Generation = 0
        };

        /// <summary>
        /// Checks if this handle is null (Id == NULL_INDEX).
        /// Generation is NOT checked because generation 0 is valid.
        /// For full validation (including generation), use pool.IsValid(handle).
        /// </summary>
        public bool IsNull => Id == HandleConfig.NULL_INDEX;

        /// <summary>
        /// Pack Handle into a single 32-bit integer for Lua interop.
        /// Used in DCS_CreateComponent, DCS_GetField, etc.
        /// </summary>
        public int Pack()
        {
            return (Id & HandleConfig.ID_MASK) | ((Generation & HandleConfig.GEN_MASK) << HandleConfig.GEN_SHIFT);
        }

        /// <summary>
        /// Unpack a 32-bit integer back into a Handle struct.
        /// Called when Lua passes a packed handle back to C#.
        /// </summary>
        public static Handle Unpack(int packed)
        {
            return new Handle
            {
                Id = (ushort)(packed & HandleConfig.ID_MASK),
                Generation = (ushort)((packed >> HandleConfig.GEN_SHIFT) & HandleConfig.GEN_MASK)
            };
        }

        /// <summary>
        /// Constructor for creating a Handle from a packed int.
        /// </summary>
        public Handle(int packed)
        {
            Id = (ushort)(packed & HandleConfig.ID_MASK);
            Generation = (ushort)((packed >> HandleConfig.GEN_SHIFT) & HandleConfig.GEN_MASK);
        }

        /// <summary>
        /// Constructor for creating a Handle from explicit ID and Generation.
        /// </summary>
        public Handle(ushort id, ushort generation)
        {
            Id = id;
            Generation = generation;
        }

        /// <summary>
        /// Creates a new Handle with incremented generation.
        /// Used when reusing a slot after free.
        /// </summary>
        public Handle NextGeneration()
        {
            return new Handle(Id, (ushort)(Generation + 1));
        }

        /// <summary>
        /// For debugging purposes only.
        /// </summary>
        public override string ToString() => Id == HandleConfig.NULL_INDEX
            ? "Handle(NULL)"
            : $"Handle(Id:{Id}, Gen:{Generation})";
    }
}