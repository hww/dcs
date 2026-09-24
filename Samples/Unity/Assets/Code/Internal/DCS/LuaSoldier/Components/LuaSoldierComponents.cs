using DCS.Core;
using UnityEngine;

namespace DCS.LuaSoldier
{
    // ============================================================
    //  МАРКЕРЫ
    // ============================================================

    /// <summary>Маркер: этот хост — солдат (управляемый персонаж).</summary>
    [ComponentPool(1000)]
    public struct SoldierTag : IComponent
    {
        public int RosterIndex { get; set; }
    }

    /// <summary>Маркер: игрок (управляется мышью/клавиатурой).</summary>
    [ComponentPool(100)]
    public struct PlayerTag : IComponent
    {
        public int RosterIndex { get; set; }
    }

    // ============================================================
    //  ПОЗИЦИЯ И ВИД
    // ============================================================

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

    /// <summary>Связка Host ↔ GameObject в сцене.</summary>
    [ComponentPool(1000)]
    public struct ViewComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public int ViewId;
        public Actor Actor;
    }

    /// <summary>Yaw/Pitch — куда смотрит солдат.</summary>
    [ComponentPool(1000)]
    public struct LookComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public float Yaw;
        public float Pitch;
    }

    // ============================================================
    //  INPUT — источники управления
    //  Lua решает, какой компонент висит на хосте.
    // ============================================================

    /// <summary>Input от клавиатуры (WASD, мышь).</summary>
    [ComponentPool(1000)]
    public struct KeyboardInputComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public float Forward;    // -1..1
        public float Strafe;     // -0.5..0.5
        public bool Fire;
    }

    /// <summary>Input от AI (пишет Lua, C# только читает).</summary>
    [ComponentPool(1000)]
    public struct AIInputComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public float Forward;
        public float Strafe;
        public bool Fire;
        public Vector3 AimTarget;
    }

    /// <summary>Input в воде (пример расширения).</summary>
    [ComponentPool(1000)]
    public struct WaterInputComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public float Forward;
        public float Strafe;
    }

    // ============================================================
    //  АНИМАЦИЯ — какой компонент висит, то и передаётся в Animator
    // ============================================================

    /// <summary>Анимация на земле.</summary>
    [ComponentPool(1000)]
    public struct GroundedAnimationComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public int Locomotion;   // 0=idle, 1=run, 2=shoot
        public int Combat;       // 0=combat, 1=guard
    }

    /// <summary>Анимация падения.</summary>
    [ComponentPool(1000)]
    public struct FallingAnimationComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public int FallType;     // 0=free fall, 1=wall slide
    }

    // ============================================================
    //  ВНУТРЕННИЕ ФЛАГИ (пишет C#)
    // ============================================================

    /// <summary>Солдат на земле (пишет C# по результатам физики).</summary>
    [ComponentPool(1000)]
    public struct GroundedTag : IComponent
    {
        public int RosterIndex { get; set; }
    }
}