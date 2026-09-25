namespace DCS.Core
{
    /// <summary>
    /// Configuration for the Lua entry point of a scene object.
    /// Contains ONLY information about what to run in Lua.
    /// No search tags, no radii, no prefabs — those live elsewhere.
    /// </summary>
    [System.Serializable]
    public struct LuaConfig
    {
        [UnityEngine.Tooltip(
            "Lua module name without .lua extension. " +
            "Example: 'actors.soldier'")]
        public string Module;

        [UnityEngine.Tooltip(
            "Function name inside the module. " +
            "Example: 'spawn_soldier'. " +
            "If empty, the module is only loaded (File mode).")]
        public string Entry;

        public bool IsValid => !string.IsNullOrEmpty(Module);


    }
}