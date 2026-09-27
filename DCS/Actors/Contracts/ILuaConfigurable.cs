using DCS.Lua;
using DCS.Spatial;
using System;
using System.Text;
using UnityEngine;

namespace DCS.Actors
{
    /// <summary>
    /// Contract for any object that carries a Lua entry point configuration.
    /// Not all BaseActor descendants need a valid Lua config — Locator, for
    /// example, only exists as a searchable coordinate marker.
    /// </summary>
    public interface ILuaConfigurable
    {
        /// <summary>Lua entry point configuration. May be invalid (empty).</summary>
        LuaConfig LuaConfig { get; }

        /// <summary>True if the Lua config is valid and should be executed.</summary>
        bool HasLuaConfig { get; }
    }

}