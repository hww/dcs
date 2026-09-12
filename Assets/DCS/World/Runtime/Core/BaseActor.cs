using DynamicComponent.Lua;
using System;
using System.Text;
using Unity.AI.Navigation.LowLevel;
using UnityEngine;

namespace DynamicComponent
{
    public abstract class BaseActor : MonoBehaviour, IFieldAccess, IFactAccess, IHostRefference, ILifeCycle, IInspectable
    {
        [Header("DCS Linking")]
        [Tooltip("ID хоста в вашем дата-ориентированном ядре.")]
        [SerializeField] protected Host _host;

        // ================================================
        // IHostRefference
        // ================================================

        /// <summary>
        /// Get host
        /// </summary>
        public Host Host => _host;

        /// <summary>
        /// Attach to the host
        /// </summary>
        /// <param name="host"></param>
        public void LinkToHost(Host host) => _host = host;

        /// <summary>
        /// Detacj from host
        /// </summary>
        public void UnlinkFromHost() => _host.Id = HandleConfig.NULL_INDEX;

        // ================================================
        // IFieldAccess
        // ================================================

        /// <summary>
        /// Reads a field from a component and pushes it to Lua stack.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        public virtual bool GetField(string fieldName, IntPtr L)
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
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Reads a value from Lua stack and writes it to a component field.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        public virtual bool SetField(string fieldName, IntPtr L)
        {
            switch (fieldName)
            {
                case "position":
                    // Читаем из стека Lua напрямую в структуру Vector3
                    float z = (float)LuaNative.lua_tonumberx(L, -1, IntPtr.Zero);
                    float y = (float)LuaNative.lua_tonumberx(L, -2, IntPtr.Zero);
                    float x = (float)LuaNative.lua_tonumberx(L, -3, IntPtr.Zero);
                    transform.position = new Vector3(x, y, z);
                    return true;
                default:
                    return false;
            }
        }

        // ================================================
        // IFactAccess
        // ================================================

        /// <summary>
        /// Reads a field from a component and pushes it to Lua stack.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        public virtual bool GetFact(string fieldName, IntPtr L)
        {
            return false;
        }

        /// <summary>
        /// Reads a value from Lua stack and writes it to a component field.
        /// Default implementation does nothing.
        /// Override in concrete pools (PositionPool, HealthPool, etc.).
        /// </summary>
        public virtual bool SetFact(string fieldName, IntPtr L)
        {
            return false;
        }

        // ================================================
        // ILifeCycle (optioal for cases when object has
        // a lyfecycle, for example the assets from store
        // ================================================

        public virtual void Birth() { }
        public virtual void Kill() { }

        // ============================================================
        //  DIAGNOSTICS & EDITOR TOOLS
        // ============================================================

        /// <summary>
        /// Prints detailed configuration metrics layout state into the Unity console.
        /// </summary>
        public virtual void Inspect(StringBuilder sb, int indentLevel)
        {
            string indent = new string(' ', indentLevel * 4);
            sb.AppendLine($"{indent}[BaseActor] Host.Id: {_host.Id} Generation: {_host.Generation}");
        }

    }
}
