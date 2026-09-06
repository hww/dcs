using System;

namespace DynamicComponent
{
    /// <summary>
    /// Configuration for packed handles (16-bit ID + 16-bit Generation = 32-bit int).
    /// 65,535 concurrent objects is enough for Ratchet & Clank scale games.
    /// We DO NOT use 64-bit handles to keep Lua interop fast and memory small.
    /// </summary>
    public static class HandleConfig
    {
        public const int ID_BITS = 16;
        public const int GEN_BITS = 16;
        public const int ID_MASK = (1 << ID_BITS) - 1;      // 0x0000FFFF (65535)
        public const int GEN_MASK = (1 << GEN_BITS) - 1;    // 0x0000FFFF (65535)
        public const int GEN_SHIFT = ID_BITS;               // 16

        /// <summary>
        /// Special null index value. 0xFFFF (65535) means "no slot".
        /// This is SAFER than using 0 because 0 is a valid index.
        /// </summary>
        public const int NULL_INDEX = ID_MASK;              // 65535

        /// <summary>
        /// Maximum number of valid indices (0 .. 65534).
        /// Index 65535 is reserved for NULL.
        /// </summary>
        public const int MAX_NUM_INDICES = 65535;
    }

    /// <summary>
    /// Safe reference to a Host (game object owner).
    /// Stored as 16-bit ID + 16-bit Generation. Packed as a 32-bit integer.
    /// Generation protects against stale references (Handle/Generation validation happens inside the Pool).
    /// </summary>
    public struct Host
    {
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
        public int Pack()
        {
            return (Id & HandleConfig.ID_MASK) | ((Generation & HandleConfig.GEN_MASK) << HandleConfig.GEN_SHIFT);
        }

        /// <summary>
        /// Unpack a 32-bit integer back into a Host struct.
        /// </summary>
        public static Host Unpack(int packed)
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

        public override string ToString() => $"Host(Id:{Id}, Gen:{Generation})";
    }

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

    /// <summary>
    /// Typed Handle for messages (events) passed between components.
    /// </summary>
    /// <remarks>
    /// Unlike Handle, TypedHandle contains TypeId for dispatching.
    /// Used in EventSystem to deliver messages to receivers.
    /// 
    /// The TypeId is stored separately (not packed) because:
    /// 1. TypeId is needed for dispatching switch/case.
    /// 2. TypeId can be up to 200 (MaxComponentTypes).
    /// 3. Packing TypeId with Handle would reduce ID/Gen bits.
    /// 
    /// When passing to Lua, we pack only Id+Generation into a 32-bit int.
    /// TypeId is passed as a separate argument.
    /// </summary>
    public struct TypedHandle
    {
        /// <summary>
        /// Base handle (Id + Generation).
        /// </summary>
        public Handle Handle;

        /// <summary>
        /// Event type (ComponentType{T}.Id).
        /// Used for switch-based dispatching in EventSystem.
        /// </summary>
        public int TypeId;

        /// <summary>
        /// Static null typed handle instance.
        /// </summary>
        public static readonly TypedHandle Null = new TypedHandle
        {
            Handle = Handle.Null,
            TypeId = -1
        };

        /// <summary>
        /// Checks whether the message handle is null (Id == NULL_INDEX).
        /// </summary>
        public bool IsNull => Handle.IsNull;

        /// <summary>
        /// Constructor for creating a TypedHandle from explicit components.
        /// </summary>
        public TypedHandle(ushort id, ushort generation, int typeId)
        {
            Handle = new Handle(id, generation);
            TypeId = typeId;
        }

        /// <summary>
        /// Constructor for creating a TypedHandle from a Handle and TypeId.
        /// </summary>
        public TypedHandle(Handle handle, int typeId)
        {
            Handle = handle;
            TypeId = typeId;
        }

        /// <summary>
        /// Constructor for creating a TypedHandle from a packed int and TypeId.
        /// </summary>
        public TypedHandle(int packed, int typeId)
        {
            Handle = Handle.Unpack(packed);
            TypeId = typeId;
        }

        /// <summary>
        /// Pack only the Id+Generation into a 32-bit integer for Lua interop.
        /// TypeId is NOT packed — it's passed separately to Lua.
        /// </summary>
        public int Pack()
        {
            return Handle.Pack();
        }

        /// <summary>
        /// For debugging purposes only.
        /// </summary>
        public override string ToString() => Handle.IsNull
            ? "TypedHandle(NULL)"
            : $"TypedHandle(Id:{Handle.Id}, Gen:{Handle.Generation}, TypeId:{TypeId})";
    }
}