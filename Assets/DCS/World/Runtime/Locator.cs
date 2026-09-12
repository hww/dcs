using DynamicComponent.Lua;
using System;
using System.Text;
using UnityEngine;

namespace DynamicComponent
{
    public class Locator : BaseActor
    {
        [Header("State Metadata")]
        public DynamicFacts Facts;
        public EFactsLifetime FactsLifetime = EFactsLifetime.Discard;

        public Vector3 GetWorldPosition() => transform.position;
        public Vector3 GetLocalPosition() => transform.localPosition;

        // ============================================================
        //  IFIELDACCESS
        // ============================================================
        public override bool GetField(string fieldName, IntPtr L)
        {
            if (fieldName.Equals("position", StringComparison.OrdinalIgnoreCase))
            {
                Vector3 pos = transform.position;
                LuaNative.lua_pushnumber(L, pos.x);
                LuaNative.lua_pushnumber(L, pos.y);
                LuaNative.lua_pushnumber(L, pos.z);
            }
            else if (fieldName.Equals("rotation", StringComparison.OrdinalIgnoreCase))
            {
                Vector3 rot = transform.eulerAngles;
                LuaNative.lua_pushnumber(L, rot.x);
                LuaNative.lua_pushnumber(L, rot.y);
                LuaNative.lua_pushnumber(L, rot.z);
            }
            else
            {
                return base.GetField(fieldName, L); // Каскад вверх к BaseActor
            }
            return false;
        }

        public override bool SetField(string fieldName, IntPtr L)
        {
            if (fieldName.Equals("position", StringComparison.OrdinalIgnoreCase))
            {
                if (LuaStack.TryGetVector3(L, -1, fieldName, _host, out Vector3 targetPos))
                {
                    transform.position = targetPos;
                }
            }
            else
            {
                return base.SetField(fieldName, L);
            }
            return false;
        }

        // ============================================================
        //  FACTS ACCESS
        // ============================================================

        /// <summary>
        /// Reads a field from a component and pushes it to Lua stack.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        public override bool GetFact(string fieldName, IntPtr L)
        {
            return Facts.GetFact(fieldName, L);
        }

        /// <summary>
        /// Reads a value from Lua stack and writes it to a component field.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        public override bool SetFact(string fieldName, IntPtr L)
        {
            return Facts.SetFact(fieldName, L);
        }

        // ============================================================
        //  DIAGNOSTICS & EDITOR TOOLS
        // ============================================================

        public override void Inspect(StringBuilder sb, int indentLevel)
        {
            base.Inspect(sb, indentLevel);
            string indent = new string(' ', (indentLevel + 1) * 4);
            sb.AppendLine($"{indent}[Locator] World Pos: {transform.position}");
        }
    }
}
