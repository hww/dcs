namespace DynamicComponent
{
    /// <summary>
    /// Safe reference to a game object (Host).
    /// </summary>
    /// <remarks>
    /// Host is the owner of components (e.g., player, enemy, bullet).
    /// Stores only an identifier and generation to protect against stale references.
    /// </remarks>
    public struct Host
    {
        public int Id;
        public int Generation;
        public bool IsNull => Generation == 0;
    }

    /// <summary>
    /// Safe reference to a component in a pool.
    /// </summary>
    /// <remarks>
    /// The handle contains an index into the Roster and a Generation.
    /// Generation is incremented each time a slot is freed, making
    /// old handles invalid (IsNull == true).
    /// This protects against accessing reused components.
    /// </remarks>
    public struct Handle
    {
        /// <summary>Index into the Roster array.</summary>
        public int Id;

        /// <summary>Roster slot generation. Incremented on each free.</summary>
        public int Generation;

        /// <summary>Checks whether the handle is valid.</summary>
        /// <returns>True if Generation is 0 (uninitialized or stale).</returns>
        public bool IsNull => Generation == 0;
    }

    /// <summary>
    /// Handle for messages (events) passed between components.
    /// </summary>
    /// <remarks>
    /// Unlike DcsHandle, contains TypeId for dispatching.
    /// Used in EventSystem to deliver messages to receivers.
    /// </remarks>
    public struct TypedHandle
    {
        /// <summary>
        /// Event type (ComponentType{T}.Id).
        /// Used for switch-based dispatching.
        /// </summary>
        public int TypeId;

        /// <summary>Roster index of the source component.</summary>
        public int Id;

        /// <summary>Generation of the source component.</summary>
        public int Generation;

        /// <summary>Checks whether the message handle is valid.</summary>
        public bool IsNull => Generation == 0;
    }
}