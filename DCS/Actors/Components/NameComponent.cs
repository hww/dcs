using DCS.Core;
using System;

namespace DCS.Actors
{
    /// <summary>
    /// Human-readable identifier for a Host.
    /// One NameComponent per Host. Not unique by default — uniqueness is enforced
    /// by the NameIndex (see below) only if you explicitly register through it.
    /// </summary>
    [ComponentPool(4096)]
    public struct NameComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public string Name;
    }
}