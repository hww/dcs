#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using DCS.Core;
using UnityEditor;
using UnityEngine;

namespace DCS.EditorTools
{
    /// <summary>
    /// Editor-инспектор DCS-хостов.
    /// Edit Mode  — показывает authoring-слой (MonoBehaviour'ы).
    /// Play Mode  — дополнительно показывает HostChain: типы, handles, поля,
    ///              вложенные структуры, состояние пула и RosterItem.
    /// </summary>
    public sealed class DCSInspectorWindow : EditorWindow
    {
        // ------------------------------------------------------------
        //  Настройки окна
        // ------------------------------------------------------------
        private Vector2 _scroll;
        private bool _autoRefresh = true;
        private double _nextRefresh;
        private const double RefreshInterval = 0.5;

        private bool _showFields = true;
        private bool _showRoster = true;
        private bool _showEmptyPools = false;

        // Ключ — (EntityId GameObject, Id домена). EntityId — структура,
        // корректно работает как часть составного ключа.
        private readonly Dictionary<(EntityId goId, int domainId), bool> _foldouts = new();

        // ------------------------------------------------------------
        //  Кэши рефлексии
        // ------------------------------------------------------------
        private static readonly Dictionary<Type, FieldInfo> _componentsFieldCache = new();
        private static readonly Dictionary<Type, FieldInfo> _partitionFieldCache = new();
        private static readonly Dictionary<Type, FieldInfo> _rosterFieldCache = new();
        private static readonly Dictionary<Type, FieldInfo[]> _publicFieldsCache = new();

        // ------------------------------------------------------------
        //  Меню и открытие
        // ------------------------------------------------------------
        [MenuItem("DCS/Inspector")]
        public static void Open()
        {
            var w = GetWindow<DCSInspectorWindow>("DCS Inspector");
            w.minSize = new Vector2(440, 320);
            w.Show();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnSelectionChanged() => Repaint();
        private void OnPlayModeChanged(PlayModeStateChange s) => Repaint();

        // ------------------------------------------------------------
        //  Update (только в Play Mode)
        // ------------------------------------------------------------
        private void Update()
        {
            if (!_autoRefresh || !Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup < _nextRefresh) return;
            _nextRefresh = EditorApplication.timeSinceStartup + RefreshInterval;
            Repaint();
        }

        // ------------------------------------------------------------
        //  OnGUI
        // ------------------------------------------------------------
        private void OnGUI()
        {
            DrawToolbar();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            var selection = Selection.gameObjects;
            if (selection == null || selection.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "Выделите один или несколько GameObject'ов.",
                    MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            for (int i = 0; i < selection.Length; i++)
            {
                if (i > 0) EditorGUILayout.Space(6);
                DrawGameObject(selection[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _autoRefresh = GUILayout.Toggle(
                    _autoRefresh, "Auto Refresh",
                    EditorStyles.toolbarButton, GUILayout.Width(100));
                _showFields = GUILayout.Toggle(
                    _showFields, "Fields",
                    EditorStyles.toolbarButton, GUILayout.Width(60));
                _showRoster = GUILayout.Toggle(
                    _showRoster, "Roster",
                    EditorStyles.toolbarButton, GUILayout.Width(60));
                _showEmptyPools = GUILayout.Toggle(
                    _showEmptyPools, "Empty Pools",
                    EditorStyles.toolbarButton, GUILayout.Width(90));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(
                        "Refresh",
                        EditorStyles.toolbarButton,
                        GUILayout.Width(70)))
                {
                    Repaint();
                }
            }
        }

        // ------------------------------------------------------------
        //  Отрисовка одного GameObject
        // ------------------------------------------------------------
        private void DrawGameObject(GameObject go)
        {
            if (go == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(go.name, EditorStyles.boldLabel);
                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                    EditorGUIUtility.PingObject(go);
            }

            DrawAuthoringLayer(go);

            var reference = go.GetComponent<IHostReference>();
            if (reference == null)
            {
                EditorGUILayout.LabelField(
                    "(нет IHostReference)", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            Host host = reference.Host;

            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField(
                    $"Host (authoring): Id={host.Id}, Gen={host.Generation}",
                    EditorStyles.miniLabel);
                EditorGUILayout.HelpBox(
                    "Runtime-данные доступны только в Play Mode.",
                    MessageType.None);
                EditorGUILayout.EndVertical();
                return;
            }

            if (!HostManager.IsValid(host))
            {
                EditorGUILayout.HelpBox(
                    $"Host({host.Id}, gen={host.Generation}) — INVALID (stale).",
                    MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField(
                $"Host: Id={host.Id}, Gen={host.Generation}",
                EditorStyles.boldLabel);

            int domainCount = DomainRegistry.Count;
            if (domainCount == 0)
            {
                EditorGUILayout.LabelField("(нет доменов)", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            for (int d = 0; d < domainCount; d++)
            {
                var domain = DomainRegistry.Get(d);
                if (domain == null) continue;
                DrawDomain(go, host, domain);
            }

            EditorGUILayout.EndVertical();
        }

        // ------------------------------------------------------------
        //  Authoring-слой
        // ------------------------------------------------------------
        private void DrawAuthoringLayer(GameObject go)
        {
            var behaviours = go.GetComponents<MonoBehaviour>();
            bool any = false;
            for (int i = 0; i < behaviours.Length; i++)
            {
                var mb = behaviours[i];
                if (mb == null) continue;
                if (mb is IFieldAccess
                 || mb is IFactAccess
                 || mb is ILifeCycle
                 || mb is IInspectable)
                {
                    if (!any)
                    {
                        EditorGUILayout.LabelField(
                            "Authoring:", EditorStyles.miniBoldLabel);
                        any = true;
                    }
                    EditorGUILayout.LabelField(
                        "  • " + mb.GetType().Name, EditorStyles.miniLabel);
                }
            }
        }

        // ------------------------------------------------------------
        //  Runtime-слой: домен → цепочка компонентов
        // ------------------------------------------------------------
        private void DrawDomain(GameObject go, Host host, Domain domain)
        {
            HostChain chain = domain.HostChain;
            if (chain == null) return;

            int count = 0;
            int currentIndex = HostManager.GlobalHosts[host.Id].FirstComponent;
            while (currentIndex >= 0)
            {
                ref ChainNode n = ref chain.GetNodeByIndex(currentIndex);
                count++;
                currentIndex = n.Next;
            }

            if (!_showEmptyPools && count == 0)
                return;

            var key = (go.GetEntityId(), domain.Id);
            if (!_foldouts.TryGetValue(key, out bool fold))
                fold = true;
            fold = EditorGUILayout.Foldout(
                fold,
                $"Domain [{domain.Id}] '{domain.Name}' — {count} components",
                true);
            _foldouts[key] = fold;
            if (!fold) return;

            EditorGUI.indentLevel++;

            currentIndex = HostManager.GlobalHosts[host.Id].FirstComponent;
            if (currentIndex < 0)
            {
                EditorGUILayout.LabelField("(цепочка пуста)", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                return;
            }

            int idx = 0;
            while (currentIndex >= 0)
            {
                ref ChainNode node = ref chain.GetNodeByIndex(currentIndex);
                DrawChainNode(node, idx++);
                currentIndex = node.Next;
            }

            EditorGUI.indentLevel--;
        }

        // ------------------------------------------------------------
        //  Отрисовка одного ChainNode
        // ------------------------------------------------------------
        private void DrawChainNode(ChainNode node, int ordinal)
        {
            string typeName = ComponentRegistry.GetTypeNameById(node.TypeId);
            if (string.IsNullOrEmpty(typeName))
                typeName = $"<type {node.TypeId}>";

            bool isStale = false;
            IComponentPool pool = null;
            int denseIndex = -1;
            int partition = -1;

            if (node.TypeId >= 0 && node.TypeId < ComponentRegistry.Pools.Length)
            {
                pool = ComponentRegistry.Pools[node.TypeId];
                if (pool != null)
                {
                    partition = GetPoolPartition(pool);
                    if (!pool.TryGetDenseIndex(node.Component, out denseIndex))
                        isStale = true;
                }
            }

            var prevBg = GUI.backgroundColor;
            if (isStale) GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = prevBg;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    $"{ordinal}. {typeName}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                string meta = partition >= 0
                    ? $"handle={node.Component.Id}/{node.Component.Generation}  pool={partition}"
                    : $"handle={node.Component.Id}/{node.Component.Generation}";
                EditorGUILayout.LabelField(
                    meta, EditorStyles.miniLabel, GUILayout.Width(220));
            }

            if (node.TypeId < 0 || node.TypeId >= ComponentRegistry.Pools.Length)
            {
                EditorGUILayout.LabelField(
                    "(typeId вне диапазона)", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            if (pool == null)
            {
                EditorGUILayout.LabelField("(pool == null)", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            if (isStale)
            {
                EditorGUILayout.HelpBox(
                    "Handle stale для этого пула.",
                    MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            if (_showRoster)
                DrawRosterRow(pool, node.Component.Id);

            if (_showFields)
                DrawFields(pool, denseIndex);

            EditorGUILayout.EndVertical();
        }

        // ------------------------------------------------------------
        //  Roster-строка
        // ------------------------------------------------------------
        private void DrawRosterRow(IComponentPool pool, int rosterIndex)
        {
            var roster = TryGetRosterArray(pool);
            if (roster == null) return;
            if (rosterIndex < 0 || rosterIndex >= roster.Length) return;

            object item = roster.GetValue(rosterIndex);
            if (item == null) return;

            Type t = item.GetType();
            var fIdx = t.GetField("Index");
            var fGen = t.GetField("Generation");
            var fNxt = t.GetField("Next");

            object idx = fIdx?.GetValue(item);
            object gen = fGen?.GetValue(item);
            object nxt = fNxt?.GetValue(item);

            EditorGUILayout.LabelField(
                $"roster[{rosterIndex}] → dense={idx}, gen={gen}, next={nxt}",
                EditorStyles.miniLabel);
        }

        // ------------------------------------------------------------
        //  Поля компонента (рекурсивно)
        // ------------------------------------------------------------
        private void DrawFields(IComponentPool pool, int denseIndex)
        {
            object boxed = TryGetBoxedComponent(pool, denseIndex);
            if (boxed == null)
            {
                EditorGUILayout.LabelField(
                    "(не удалось получить компонент)", EditorStyles.miniLabel);
                return;
            }

            Type t = boxed.GetType();
            FieldInfo[] fields = GetPublicInstanceFields(t);

            // Отфильтровываем технический RosterIndex
            int visible = 0;
            for (int i = 0; i < fields.Length; i++)
                if (fields[i].Name != "RosterIndex") visible++;

            if (visible == 0)
            {
                bool onlyRoster = fields.Length == 1
                    && fields[0].Name == "RosterIndex";
                EditorGUILayout.LabelField(
                    onlyRoster
                        ? "(маркер, только RosterIndex)"
                        : "(нет публичных полей)",
                    EditorStyles.miniLabel);
                return;
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < fields.Length; i++)
            {
                var f = fields[i];
                if (f.Name == "RosterIndex") continue;

                object value;
                try { value = f.GetValue(boxed); }
                catch { value = "<error>"; }

                DrawValueRow(f.Name, value, 0);
            }
            EditorGUI.indentLevel--;
        }

        // Рекурсивная отрисовка значения.
        // Вложенные структуры (не примитивы и не известные Unity-типы)
        // разворачиваются в подполе.
        private void DrawValueRow(string name, object value, int depth)
        {
            if (value == null)
            {
                DrawSimpleRow(name, "null", depth);
                return;
            }

            Type t = value.GetType();
            if (!IsNestedStruct(t))
            {
                DrawSimpleRow(name, FormatValue(value), depth);
                return;
            }

            // Заголовок вложенной структуры
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(depth * 12);
                EditorGUILayout.LabelField(
                    name, EditorStyles.miniBoldLabel, GUILayout.Width(160 - depth * 12));
            }

            FieldInfo[] subFields = GetPublicInstanceFields(t);
            for (int i = 0; i < subFields.Length; i++)
            {
                object subVal;
                try { subVal = subFields[i].GetValue(value); }
                catch { subVal = "<error>"; }
                DrawValueRow(subFields[i].Name, subVal, depth + 1);
            }
        }

        private void DrawSimpleRow(string name, string text, int depth)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(depth * 12);
                EditorGUILayout.LabelField(
                    name, GUILayout.Width(160 - depth * 12));
                EditorGUILayout.SelectableLabel(
                    text, EditorStyles.miniLabel, GUILayout.Height(16));
            }
        }

        private static bool IsNestedStruct(Type t)
        {
            if (!t.IsValueType) return false;
            if (t.IsPrimitive) return false;
            if (t.IsEnum) return false;
            if (t == typeof(decimal)) return false;
            if (t == typeof(IntPtr)) return false;
            if (t == typeof(UIntPtr)) return false;
            if (t == typeof(Vector2)) return false;
            if (t == typeof(Vector3)) return false;
            if (t == typeof(Vector4)) return false;
            if (t == typeof(Quaternion)) return false;
            if (t == typeof(Color)) return false;
            if (t == typeof(Color32)) return false;
            if (t == typeof(Rect)) return false;
            if (t == typeof(Bounds)) return false;
            // Не раскрываем сам IComponent — у него только RosterIndex
            if (typeof(IComponent).IsAssignableFrom(t)) return false;
            return true;
        }

        private static string FormatValue(object value)
        {
            if (value == null) return "null";
            if (value is string s) return s;
            if (value is bool b) return b ? "true" : "false";
            if (value is float f) return f.ToString("0.#####");
            if (value is double d) return d.ToString("0.#####");
            if (value is Vector3 v3)
                return $"({v3.x:0.###}, {v3.y:0.###}, {v3.z:0.###})";
            if (value is Vector2 v2)
                return $"({v2.x:0.###}, {v2.y:0.###})";
            if (value is Vector4 v4)
                return $"({v4.x:0.###}, {v4.y:0.###}, {v4.z:0.###}, {v4.w:0.###})";
            if (value is Quaternion q)
                return $"({q.x:0.##}, {q.y:0.##}, {q.z:0.##}, {q.w:0.##})";
            if (value is Color c)
                return $"RGBA({c.r:0.##}, {c.g:0.##}, {c.b:0.##}, {c.a:0.##})";
            return value.ToString();
        }

        // ------------------------------------------------------------
        //  Рефлексия по пулам
        // ------------------------------------------------------------

        // Находит generic ComponentPool<> в иерархии типа пула.
        private static Type FindComponentPoolBase(Type t)
        {
            while (t != null)
            {
                if (t.IsGenericType
                    && t.GetGenericTypeDefinition() == typeof(ComponentPool<>))
                    return t;
                t = t.BaseType;
            }
            return null;
        }

        private static FieldInfo GetCachedField(
            Dictionary<Type, FieldInfo> cache,
            Type t,
            string fieldName)
        {
            if (t == null) return null;
            if (!cache.TryGetValue(t, out var fi))
            {
                fi = t.GetField(
                    fieldName,
                    BindingFlags.Public | BindingFlags.Instance);
                cache[t] = fi;
            }
            return fi;
        }

        private static FieldInfo[] GetPublicInstanceFields(Type t)
        {
            if (t == null) return Array.Empty<FieldInfo>();
            if (!_publicFieldsCache.TryGetValue(t, out var arr))
            {
                arr = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
                _publicFieldsCache[t] = arr;
            }
            return arr;
        }

        private static object TryGetBoxedComponent(
            IComponentPool pool, int denseIndex)
        {
            if (pool == null) return null;
            Type t = FindComponentPoolBase(pool.GetType());
            if (t == null) return null;

            var fi = GetCachedField(_componentsFieldCache, t, "Components");
            if (fi == null) return null;

            var arr = fi.GetValue(pool) as Array;
            if (arr == null || denseIndex < 0 || denseIndex >= arr.Length)
                return null;
            return arr.GetValue(denseIndex); // boxed copy
        }

        private static Array TryGetRosterArray(IComponentPool pool)
        {
            if (pool == null) return null;
            Type t = FindComponentPoolBase(pool.GetType());
            if (t == null) return null;

            var fi = GetCachedField(_rosterFieldCache, t, "Roster");
            if (fi == null) return null;
            return fi.GetValue(pool) as Array;
        }

        private static int GetPoolPartition(IComponentPool pool)
        {
            if (pool == null) return -1;
            Type t = FindComponentPoolBase(pool.GetType());
            if (t == null) return -1;

            var fi = GetCachedField(_partitionFieldCache, t, "Partition");
            if (fi == null) return -1;

            object raw = fi.GetValue(pool);
            if (raw == null) return -1;
            // Partition — ushort в ComponentPool<T>
            try { return Convert.ToInt32(raw); }
            catch { return -1; }
        }
    }
}
#endif