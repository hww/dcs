using DCS.Core;
using UnityEngine;

namespace DCS.Game.SoldierLua
{
    [ComponentPool(1000)]
    public struct AnimationStateComponent : IComponent, IInitializable
    {
        public int RosterIndex { get; set; }

        public Handle PositionHandle;
        public Handle LookHandle;
        public Handle KeyboardInputHandle;

        public Animator Animator;

        public int Locomotion;
        public int Combat;
        public int FallType;
        public bool IsFalling;

        public void Init(object prius)
        {
            Debug.Log($"[Anim.Init] prius={(prius?.GetType().Name ?? "null")}");
            if (prius is SoldierPrius p)
            {
                Debug.Log($"[Anim.Init] p.Actor={(p.Actor != null)}, " +
                          $"p.Actor.Animator={(p.Actor?.Animator != null)}");
                if (p.Actor != null)
                    Animator = p.Actor.Animator;
            }
        }
    }
}