using DCS.Core;
using DCS.Lua;
using DCS.Spatial;
using System;
using System.Text;
using UnityEngine;

namespace DCS.Actors
{
    public abstract class BaseActor : MonoBehaviour,
                                 IFieldAccess, IFactAccess, IHostReference,
                                 ILifeCycle, ILuaHookable, ISearchable,
                                 IInspectable
    {
        // ==================== Host Link ====================
        [Header("DCS Linking")]
        [SerializeField] protected Host _host;
        public Host Host => _host;
        public void LinkToHost(Host host) => _host = host;
        public void UnlinkFromHost() => _host.Id = HandleConfig.NULL_INDEX;

        // ==================== Life Cycle ====================
        [Header("Cached References (filled in Birth)")]
        [SerializeField] protected Transform _cachedTransform;
        [SerializeField] protected Animator _cachedAnimator;
        public Transform CachedTransform => _cachedTransform;
        public Animator CachedAnimator => _cachedAnimator;

        [System.NonSerialized] private bool _born;
        [System.NonSerialized] private bool _killed;
        public bool IsBorn => _born;
        public bool IsKilled => _killed;

        // ==================== Lua ====================
        [Header("Lua (optional)")]
        [SerializeField] protected LuaHook _LuaHook;
        public LuaHook LuaHook => _LuaHook;
        public virtual bool HasLuaHook => _LuaHook.IsValid;

        // ==================== Search ====================
        [Header("Search")]

        [Tooltip("Semantic type used for type-filtered queries.")]
        [SerializeField] protected ESpatialObjectType _objectType = ESpatialObjectType.Generic;

        [Tooltip("Comma-separated tags, no spaces. Example: 'german,patrol'.")]
        [SerializeField] protected string _archetype;

        public string NameString => name;

        public ESpatialObjectType ObjectType => _objectType;

        public string Archetype => _archetype;

        public bool IsSearchable => !string.IsNullOrEmpty(name) || Archetype.Length > 0;

        public bool IsAlive => this != null && gameObject != null;

        // ==================== Life Cycle ====================
        public virtual void Birth()
        {
            if (_born) return;
            if (!HostManager.IsValid(_host))
            {
                Debug.LogError($"[BaseActor] Birth без валидного Host: {name}");
                return;
            }
            _cachedTransform = transform;
            _cachedAnimator = GetComponentInChildren<Animator>(true);
            _born = true;
        }

        public virtual void Kill()
        {
            if (!_born || _killed) return;
            _cachedTransform = null;
            _cachedAnimator = null;
            _killed = true;
        }

        // ==================== Field Access ====================
        public virtual bool GetField(string fieldName, IntPtr L)
        {
            switch (fieldName)
            {
                case "position":
                    Vector3 pos = transform.position;
                    LuaNative.lua_pushnumber(L, pos.x);
                    LuaNative.lua_pushnumber(L, pos.y);
                    LuaNative.lua_pushnumber(L, pos.z);
                    return true;
                default:
                    return false;
            }
        }

        public virtual bool SetField(string fieldName, IntPtr L)
        {
            switch (fieldName)
            {
                case "position":
                    double z = LuaNative.lua_tonumberx(L, -1, IntPtr.Zero);
                    double y = LuaNative.lua_tonumberx(L, -2, IntPtr.Zero);
                    double x = LuaNative.lua_tonumberx(L, -3, IntPtr.Zero);
                    transform.position = new Vector3((float)x, (float)y, (float)z);
                    return true;
                default:
                    return false;
            }
        }

        // ==================== Facts ====================
        public virtual bool GetFact(string fieldName, IntPtr L) => false;
        public virtual bool SetFact(string fieldName, IntPtr L) => false;

        // ==================== Inspect ====================
        public virtual void Inspect(StringBuilder sb, int indentLevel)
        {
            string indent = new string(' ', indentLevel * 4);
            sb.AppendLine($"{indent}[BaseActor] Host.Id: {_host.Id} Gen: {_host.Generation}");
            sb.AppendLine($"{indent}  SearchName: '{name}' Type: {_objectType} " +
                          $"Archetype: '{_archetype}'");
            sb.AppendLine($"{indent}  Lua: Module='{_LuaHook.Module}' Entry='{_LuaHook.Entry}' " +
                          $"HasLuaHook={HasLuaHook}");
        }
    }
}