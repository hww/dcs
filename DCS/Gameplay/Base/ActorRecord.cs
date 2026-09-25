using DCS.Spatial;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Immutable snapshot of an actor's searchable identity.
    /// Stored in the registry and used by spatial systems as a shared
    /// reference point. Contains no runtime behavior — only identity
    /// and spatial metadata.
    /// </summary>
    public struct ActorRecord
    {
        /// <summary>Stable runtime handle. Used as the key in all registries.</summary>
        public ushort Handle;

        /// <summary>Unique name. May be null for dynamically spawned actors without names.</summary>
        public string Name;

        /// <summary>Semantic type.</summary>
        public ESpatialObjectType ObjectType;

        /// <summary>Cached tag array. Never null.</summary>
        public string[] Tags;

        /// <summary>Owner identifier. Multiple actors may share an owner (e.g. a squad).</summary>
        public ushort OwnerId;

        /// <summary>World position of the actor's origin at registration time.</summary>
        public Vector3 Position;

        /// <summary>Scene identifier. 0 = dynamic, >0 = static scene.</summary>
        public ushort SceneId;

        /// <summary>True if this actor is registered in the spatial hash.</summary>
        public bool InSpatial;

        /// <summary>True if this actor is currently active (simulated).</summary>
        public bool Active;
    }
}