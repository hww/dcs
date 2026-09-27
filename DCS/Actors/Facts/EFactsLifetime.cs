namespace DCS.Actors
{
    /// <summary>
    /// Defines the lifetime scope for game facts (state data) within the game world.
    /// Determines when the data should be automatically cleaned up based on game events
    /// such as level unloading, scene transitions, or session restarts.
    /// </summary>
    public enum EFactsLifetime
    {
        /// <summary>
        /// Reset and wipe facts as soon as the scene unloads from memory
        /// </summary>
        Discard,
        /// <summary>
        /// Persist throughout the current map region bounds lifecycle
        /// </summary>
        Location,
        /// <summary>
        /// Serialize directly into global save game footprint states
        /// </summary>
        Persistent,
        /// <summary>
        /// Live strictly until the application process termination
        /// </summary>
        Session
    }
}