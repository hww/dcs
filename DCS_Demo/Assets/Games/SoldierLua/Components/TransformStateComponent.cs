using DCS.Actors;
using DCS.Core;

namespace DCS.Game.SoldierLua
{
    [ComponentPool(1000)]
    public struct TransformStateComponent : IComponent, IInitializable
    {
        public int RosterIndex { get; set; }

        public Handle PositionHandle;
        public Handle LookHandle;

        public Actor Actor;

        public void Init(object prius)
        {
            if (prius is SoldierPrius p)
                Actor = p.Actor;
        }
    }
}