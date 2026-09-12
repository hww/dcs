using DynamicComponent.Lua;
using System;
using System.Text;
using UnityEngine;

namespace DynamicComponent
{
    /// <summary>
    /// Spawner — passive spawn configuration point data anchor.
    /// Contains target attributes read by Top-Down Lua script directors to allocate DCS entities.
    /// </summary>
    public class Spawner : Locator
    {
        public enum ESpawnMode
        {
            SpawnByCode,        // Allocated strictly via execution requests from Lua script code
            SpawnOnLoadScene,   // Automated baseline allocation on scene load sequence triggers
            SpawnOnSelectZone,  // Bootstrapped during spatial zone selection routines
            AutoSpawn           // Script-driven polling evaluation
        }

        [Header("Spawn Conditions")]
        public ESpawnMode Mode = ESpawnMode.SpawnByCode;
        public float radius = 10f;
        public float deSpawnRadius = 12f;
        public string entityClassToSpawn;

        // ============================================================
        //  IFIELDACCESS: READ INTERFACE (GET)
        // ============================================================
        public override bool GetField(string fieldName, IntPtr L)
        {
            // Используем быстрый Ordinal-свитч строк без аллокаций
            switch (fieldName)
            {
                case "spawnMode":
                    LuaNative.lua_pushinteger(L, (int)Mode);
                    break;
                case "spawnRadius":
                    LuaNative.lua_pushnumber(L, radius);
                    break;
                case "deSpawnRadius":
                    LuaNative.lua_pushnumber(L, deSpawnRadius);
                    break;
                case "entityClass":
                    // Заменяем нативный метод на безопасный пуш (предполагается наличие хелпера)
                    LuaNative.lua_pushstring(L, entityClassToSpawn ?? string.Empty);
                    break;
                case "factsLifetime":
                    LuaNative.lua_pushinteger(L, (int)FactsLifetime);
                    break;
                default:
                    // Если поле не специфично для спаунера, каскадно пробрасываем вверх к Locator/BaseActor
                    return base.GetField(fieldName, L);
            }
            return false;
        }

        // ============================================================
        //  IFIELDACCESS: WRITE INTERFACE (SET)
        // ============================================================
        public override bool SetField(string fieldName, IntPtr L)
        {
            switch (fieldName)
            {
                // Жесткий API (TryGet) с автоматической генерацией ошибок для критичных данных
                case "spawnRadius":
                    if (LuaStack.TryGetFloat(L, -1, fieldName, _host, out float _radius))
                        radius = _radius;
                    break;

                case "deSpawnRadius":
                    if (LuaStack.TryGetFloat(L, -1, fieldName, _host, out float _deRadius))
                        deSpawnRadius = _deRadius;
                    break;

                case "entityClass":
                    if (LuaStack.TryGetString(L, -1, fieldName, _host, out string entityClass))
                        entityClassToSpawn = entityClass;
                    break;

                // Мягкий API (OrDefault): при некорректном типе из Lua безопасно откатываемся к дефолту
                case "spawnMode":
                    Mode = (ESpawnMode)LuaStack.GetIntOrDefault(L, -1, (int)ESpawnMode.SpawnByCode);
                    break;

                case "factsLifetime":
                    FactsLifetime = (EFactsLifetime)LuaStack.GetIntOrDefault(L, -1, (int)EFactsLifetime.Discard);
                    break;

                default:
                    // Каскадный проброс вверх (например, для изменения "position" через Locator)
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
            base.Inspect(sb, indentLevel); // Выведет базовую инфу о Host Id и Generation
            string indent = new string(' ', (indentLevel + 1) * 4);

            sb.AppendLine($"{indent}[Spawner] Mode: {Mode} | Class: '{entityClassToSpawn}'");
            sb.AppendLine($"{indent}Geometry -> Radius: {radius} | DeSpawnRadius: {deSpawnRadius}");
            sb.AppendLine($"{indent}Save State -> Lifetime Scope: {FactsLifetime} | Has Facts: {Facts != null}");
        }

        private void OnDrawGizmos()
        {
            if (Mode == ESpawnMode.AutoSpawn)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, radius);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, deSpawnRadius);
            }
        }
    }
}
