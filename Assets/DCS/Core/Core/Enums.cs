namespace DynamicComponent
{
    /// <summary>
    /// Update phases on the main processor (PPU).
    /// </summary>
    public enum EUpdateStage
    {
        /// <summary>No update.</summary>
        None,

        /// <summary>Main frame update (Time.deltaTime).</summary>
        Update,

        /// <summary>Fixed timestep update (Time.fixedDeltaTime).</summary>
        FixedUpdate,

        /// <summary>Post-render update.</summary>
        PostUpdate
    }

    /// <summary>
    /// Asynchronous update phases (SPU / Job System).
    /// </summary>
    public enum EAsyncUpdateStage
    {
        /// <summary>No async update.</summary>
        None,

        /// <summary>Async main frame update.</summary>
        Update,

        /// <summary>Async fixed timestep update.</summary>
        FixedUpdate,

        /// <summary>Async post-render update.</summary>
        PostUpdate
    }
}