using DCS.Actors;
using DCS.Core;
using UnityEngine;

namespace DCS.Game.SoldierLua
{
    [ComponentPool(1000)]
    public struct VelocityComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public Vector3 Value;
    }

    [ComponentPool(1000)]
    public struct LookComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public float Yaw;
        public float Pitch;
    }

    [ComponentPool(1000)]
    public struct KeyboardInputComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public float Forward;
        public float Strafe;
        public bool Fire;
        public Handle LookHandle;
    }

    [ComponentPool(1000)]
    public struct AIInputComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public float Forward;
        public float Strafe;
        public bool Fire;
        public Vector3 AimTarget;
    }

    [ComponentPool(1000)]
    public struct WaterInputComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public float Forward;
        public float Strafe;
    }
}