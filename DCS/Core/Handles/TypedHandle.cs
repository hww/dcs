using System;

namespace DCS.Core
{
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