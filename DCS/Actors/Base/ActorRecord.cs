using DCS.Spatial;
using System;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Stable reference to an entry in ActorRegistry.
    /// Generation guards against stale handles after reuse.
    /// </summary>
    [System.Serializable]
    public struct ActorHandle : IEquatable<ActorHandle>
    {
        public const ushort NULL_ID = 0;

        public ushort Id;
        public ushort Generation;

        public static readonly ActorHandle Null = new ActorHandle(NULL_ID, 0);

        public ActorHandle(ushort id, ushort generation)
        {
            Id = id;
            Generation = generation;
        }

        public ActorHandle(int packed)
        {
            Id = (ushort)(packed & 0xFFFF);
            Generation = (ushort)((packed >> 16) & 0xFFFF);
        }

        public bool IsNull => Id == NULL_ID;

        /// <summary>Pack Id and Generation into a single int (Id in low 16 bits).</summary>
        public int Pack()
        {
            return ((Generation & 0xFFFF) << 16) | (Id & 0xFFFF);
        }

        public static ActorHandle Unpack(int packed)
        {
            return new ActorHandle(packed);
        }

        public bool Equals(ActorHandle other)
        {
            return Id == other.Id && Generation == other.Generation;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Id << 16) | Generation;
        }

        public static bool operator ==(ActorHandle a, ActorHandle b) => a.Equals(b);
        public static bool operator !=(ActorHandle a, ActorHandle b) => !a.Equals(b);

        public override string ToString()
        {
            return IsNull
                ? "ActorHandle(NULL)"
                : $"ActorHandle(Id:{Id}, Gen:{Generation})";
        }
    }

    /// <summary>
    /// Immutable snapshot of an actor's searchable identity.
    /// Stored in the registry and used by spatial systems as a shared
    /// reference point. Contains no runtime behavior — only identity
    /// and spatial metadata.
    /// </summary>
    public struct ActorRecord
    {
        /// <summary>Stable runtime handle. Used as the key in all registries.</summary>
        public ActorHandle Handle;

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