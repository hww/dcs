using System;

namespace DCS.Core
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
}