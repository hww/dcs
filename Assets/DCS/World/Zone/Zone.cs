using System;
using System.Text;
using UnityEngine;
using DynamicComponent.Lua;

namespace DynamicComponent
{
    public class Zone : BaseActor
    {
        [Header("State Metadata")]
        public DynamicFacts Facts;
        public EFactsLifetime FactsLifetime = EFactsLifetime.Discard;

        [Header("Streaming Settings (Horizon Zero Dawn style)")]
        [SerializeField] private string targetSceneName;
        [SerializeField] private string zoneDirectorScriptPath = "Locations/SwampZone/swamp_zone_director.lua";

        [Header("Detection Bounds")]
        public bool NeedCollision = true;
        public float ZoneRadius = 40f;
        public float ActivationRadius = 50f;

        // ============================================================
        //  FIELD ACCESS
        // ============================================================
        public override bool GetField(string fieldName, IntPtr L)
        {
            switch (fieldName)
            {
                case "targetScene":
                    LuaNative.lua_pushstring(L, targetSceneName ?? string.Empty);
                    return true;
                case "directorScript":
                    LuaNative.lua_pushstring(L, zoneDirectorScriptPath ?? string.Empty);
                    return true;
                case "activationRadius":
                    LuaNative.lua_pushnumber(L, ActivationRadius);
                    return true;
                default:
                    return base.GetField(fieldName, L);
            }
        }

        public override bool SetField(string fieldName, IntPtr L)
        {
            switch (fieldName)
            {
                case "activationRadius":
                    if (LuaStack.TryGetFloat(L, -1, fieldName, _host, out float r)) ActivationRadius = r;
                    return true;
                default:
                    return base.SetField(fieldName, L);
            }
        }

        // ============================================================
        //  FACTS ACCESS
        // ============================================================
        public override bool GetFact(string fieldName, IntPtr L)
        {
            return Facts.GetFact(fieldName, L);
        }

        public override bool SetFact(string fieldName, IntPtr L)
        {
            return Facts.SetFact(fieldName, L);
        }

        // ============================================================
        // DEBUGGING
        // ============================================================
        private void OnDrawGizmos()
        {
            if (NeedCollision)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, ZoneRadius);

                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, ActivationRadius);
            }
        }

        public override void Inspect(StringBuilder sb, int indentLevel)
        {
            base.Inspect(sb, indentLevel);
            string indent = new string(' ', (indentLevel + 1) * 4);
            sb.AppendLine($"{indent}[Zone Details] TargetScene: {targetSceneName}");
        }
    }
}
