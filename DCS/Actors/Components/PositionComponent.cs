using DCS.Core;
using UnityEngine;

namespace DCS.Actors
{
    /// <summary>
    /// Single source of truth for world position.
    /// Every Host that needs to participate in spatial queries MUST have this.
    /// Systems write here; SpatialUpdateSystem reads here.
    /// </summary>
    [ComponentPool(16384)]
    public struct PositionComponent : IComponent
    {
        public Host Host { get; set; }
        public Vector3 Position;
        public Quaternion Rotation;
    }
}