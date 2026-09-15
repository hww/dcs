// === FILE: Examples/Core/TestbenchStructures.cs ===
using DCS.Core;
using UnityEngine;

namespace DCS.Tests.PlayMode
{
    public enum ETestAIState : byte
    {
        Idle = 0,
        Patrolling = 1,
        CombatAmbusher = 2,
        Dead = 3
    }

    [System.Serializable]
    public struct CustomStats
    {
        public int Level;
        public float CritChance;
        public bool IsBoss;
    }

    [ComponentPool(1000)]
    public struct BenchmarkComponent : IComponent
    {
        public int RosterIndex { get; set; }

        // Primitive fields
        public string EntityName;
        public bool IsActive;

        // Unity complex mathematical fields
        public Vector3 Position;
        public Color TeamColor;

        // Enum field
        public ETestAIState AIState;

        // Deeply nested custom struct field (will be parsed recursively!)
        public CustomStats Stats;
    }
}
