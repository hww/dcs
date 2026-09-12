using DynamicComponent.Lua;
using System;
using System.Text;
using UnityEngine;

namespace DynamicComponent
{
    /// <summary>
    /// Types of interactive gameplay actors.
    /// </summary>
    public enum EntityActorType { Default, Collectible, Enemy, Player, Environment }

    /// <summary>
    /// Interactive Actor — an entity backed by physics representations and visual geometry.
    /// Inherits the base transactional Entity lifecycle without running heavy automatic C# processes.
    /// </summary>
    public class Actor : BaseActor
    {
        [Header("Entity Actor - Physics")]
        public Rigidbody RigidBody;
        public Collider Collider;
        public Animator Animator;

        [Header("Entity Actor - Type")]
        public EntityActorType ActorType = EntityActorType.Default;

        [Header("Entity Actor - Visual")]
        public GameObject Visual;

        [Header("Entity Actor - Owners")]
        // Имя (идентификатор) скомпилированного датасета карты, к которой привязан этот объект
        public string LevelDatasetName;

        public Zone Zone;
        public Spawner Spawner;

        [Header("Entity Actor - Facts")]
        public DynamicFacts Facts;
        public EFactsLifetime FactsLifetime;

        // ============================================================
        //  IFieldAccess Реализация для быстрого шлюза в Lua
        // ============================================================

        public override bool GetField(string fieldName, IntPtr L)
        {
            // Используем быстрый Ordinal хэш-маппинг или switch строк
            switch (fieldName)
            {
                case "position":
                    Vector3 pos = transform.position;
                    // Пушим координаты напрямую в Lua стек через ваш API
                    LuaNative.lua_pushnumber(L, pos.x);
                    LuaNative.lua_pushnumber(L, pos.y);
                    LuaNative.lua_pushnumber(L, pos.z);
                    break;

                case "actorType":
                    // Возвращаем enum как integer
                    LuaNative.lua_pushinteger(L, (int)ActorType);
                    break;

                case "hasAnimator":
                    // Быстрая проверка без GetComponent!
                    LuaNative.lua_pushboolean(L, (Animator != null) ? 1 : 0);
                    break;
                default:
                    return base.GetField(fieldName, L); 
            }
            return false;
        }

        public override bool SetField(string fieldName, IntPtr L)
        {
            switch (fieldName)
            {
                case "position":
                    // Читаем из стека Lua напрямую в структуру Vector3
                    float z = (float)LuaNative.lua_tonumberx(L, -1, IntPtr.Zero);
                    float y = (float)LuaNative.lua_tonumberx(L, -2, IntPtr.Zero);
                    float x = (float)LuaNative.lua_tonumberx(L, -3, IntPtr.Zero);
                    transform.position = new Vector3(x, y, z);
                    break;

                case "animatorState":
                    if (Animator != null)
                    {
                        int stateNameHash = (int)LuaNative.lua_tointegerx(L, -1, IntPtr.Zero);
                        Animator.Play(stateNameHash, -1, float.NegativeInfinity);
                    }
                    break;
                default:
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

        /// <summary>
        /// Prints detailed configuration metrics layout state into the Unity console.
        /// </summary>
        public override void Inspect(StringBuilder sb, int indentLevel)
        {
            base.Inspect(sb, indentLevel);
            sb.AppendLine($"[EntityActor Details] ActorType: {ActorType}, Physics: {RigidBody != null}, " +
                     $"Collider: {Collider != null}, Visual: {Visual != null}");
        }
    }
}
