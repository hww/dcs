using DCS.Core;
using UnityEngine;

namespace DCS.Soldiers
{
    public enum ELocomotion
    {
        Idle = 0,
        Run = 1,
        Shoot = 2
    }

    public enum ECombatState
    {
        Combat = 0,
        Guard = 1
    }

    [ComponentPool(1000)]
    public struct SoldierTag : IComponent
    {
        public int RosterIndex { get; set; }
    }

    [ComponentPool(1000)]
    public struct PlayerTag : IComponent
    {
        public int RosterIndex { get; set; }
    }

    [ComponentPool(1000)]
    public struct PositionComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public Vector3 Value;
    }

    [ComponentPool(1000)]
    public struct VelocityComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public Vector3 Value;
    }

    [ComponentPool(1000)]
    public struct InputComponent : IComponent
    {
        public int RosterIndex { get; set; }

        public float Forward;   // W/S, -1..1
        public float Strafe;    // A/D, -0.5..0.5
        public bool Fire;

        public float LookYaw;   // градусы, 0..360
        public float LookPitch; // градусы, -80..80
    }

    [ComponentPool(1000)]
    public struct CombatStateComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public ECombatState Value;
    }

    [ComponentPool(1000)]
    public struct LocomotionComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public ELocomotion Value;
    }

    [ComponentPool(1000)]
    public struct ViewComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public int ViewId;
        public Actor Actor;
    }
}