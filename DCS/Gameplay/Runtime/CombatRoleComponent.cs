using DCS.Core;

namespace DCS.Gameplay
{
    /// <summary>
    /// Combat role assigned to an agent by the AI system.
    /// Allocated from Lua via AI.SetCombatRole(hostId, role, strongPointId).
    /// </summary>
    [ComponentPool(1000)]
    public struct CombatRoleComponent : IComponent
    {
        public int RosterIndex { get; set; }

        /// <summary>CombatRole enum value (None/Engager/Ambusher/Defender/GrenadeThrower/Flanker).</summary>
        public CombatRole Role;

        /// <summary>StrongPoint ID this agent is entrenched at (0 = none).</summary>
        public int StrongPointId;

        /// <summary>Host ID of the agent owning this role.</summary>
        public int OwnerHostId;
    }
}