using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DCS.Authoring.Editor
{
    [CustomEditor(typeof(ShapeBorder))]
    [CanEditMultipleObjects]
    public sealed class ShapeBorderEditor : BaseShapeEditor
    {
        private SerializedProperty _pointsProp;
        private SerializedProperty _closedProp;
        private SerializedProperty _minYProp;
        private SerializedProperty _maxYProp;

        private readonly HashSet<int> _selected = new HashSet<int>();
        private bool _showPointList = true;

        protected override void OnEnable()
        {
            base.OnEnable();
            _pointsProp = serializedObject.FindProperty("_points");
            _closedProp = serializedObject.FindProperty("_closed");
            _minYProp = serializedObject.FindProperty("_minY");
            _maxYProp = serializedObject.FindProperty("_maxY");
        }

        protected override string TargetDisplayName => "Shape Border";

        protected override void DrawShapeInspector()
        {
            EditorGUILayout.LabelField("Slab", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            float minY = EditorGUILayout.FloatField("Min Y", _minYProp.floatValue);
            float maxY = EditorGUILayout.FloatField("Max Y", _maxYProp.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                if (maxY < minY) maxY = minY;
                _minYProp.floatValue = minY;
                _maxYProp.floatValue = maxY;
            }
            EditorGUILayout.LabelField("Thickness", (maxY - minY).ToString("0.###"), EditorStyles.miniLabel);

            EditorGUI.indentLevel--;

            EditorGUI.BeginChangeCheck();
            bool closed = EditorGUILayout.ToggleLeft("Closed Loop", _closedProp.boolValue);
            if (EditorGUI.EndChangeCheck())
                _closedProp.boolValue = closed;

            EditorGUILayout.Space(4f);
            _showPointList = EditorGUILayout.Foldout(_showPointList, $"Points ({_pointsProp.arraySize})", true);
            if (_showPointList)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < _pointsProp.arraySize; i++)
                {
                    var element = _pointsProp.GetArrayElementAtIndex(i);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var prevColor = GUI.color;
                        GUI.color = _selected.Contains(i) ? ShapeEditorGUIUtility.SelectedColor : Color.white;

                        if (GUILayout.Button($"{i}", EditorStyles.miniButton, GUILayout.Width(28)))
                        {
                            if (Event.current.shift || Event.current.control)
                            {
                                if (!_selected.Add(i)) _selected.Remove(i);
                            }
                            else
                            {
                                _selected.Clear();
                                _selected.Add(i);
                            }
                            SceneView.RepaintAll();
                        }

                        GUI.color = prevColor;

                        EditorGUI.BeginChangeCheck();
                        Vector3 v = EditorGUILayout.Vector3Field(GUIContent.none, element.vector3Value);
                        if (EditorGUI.EndChangeCheck())
                            element.vector3Value = v;

                        if (GUILayout.Button("\u2715", EditorStyles.miniButton, GUILayout.Width(22)))
                        {
                            _pointsProp.DeleteArrayElementAtIndex(i);
                            _selected.Clear();
                            break;
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add Point"))
                    {
                        int idx = _pointsProp.arraySize;
                        _pointsProp.InsertArrayElementAtIndex(idx);
                        var element = _pointsProp.GetArrayElementAtIndex(idx);
                        element.vector3Value = (idx > 0)
                            ? _pointsProp.GetArrayElementAtIndex(idx - 1).vector3Value + new Vector3(1f, 0f, 0f)
                            : Vector3.zero;
                    }

                    if (GUILayout.Button("Flip Winding") && _pointsProp.arraySize > 2)
                    {
                        var values = new List<Vector3>();
                        for (int i = 0; i < _pointsProp.arraySize; i++)
                            values.Add(_pointsProp.GetArrayElementAtIndex(i).vector3Value);
                        values.Reverse();
                        for (int i = 0; i < values.Count; i++)
                            _pointsProp.GetArrayElementAtIndex(i).vector3Value = values[i];
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        protected override void DrawShapeSceneGizmos()
        {
            var border = (ShapeBorder)target;
            if (border.PointCount < 2) return;

            var prev = Handles.color;

            // ---- Отрисовка ПОЛУПРОЗРАЧНОГО ОБЪЕМА для плиты (Slab) ----
            if (border.PointCount >= 3 && border.Closed)
            {
                Handles.color = ShapeEditorGUIUtility.FillColor;

                // Создаем боковые грани (стены) объема
                for (int i = 0; i < border.PointCount; i++)
                {
                    int next = (i + 1) % border.PointCount;
                    Vector3 p1Min = ToWorld(border, border.GetPoint(i), border.MinY);
                    Vector3 p1Max = ToWorld(border, border.GetPoint(i), border.MaxY);
                    Vector3 p2Max = ToWorld(border, border.GetPoint(next), border.MaxY);
                    Vector3 p2Min = ToWorld(border, border.GetPoint(next), border.MinY);

                    Handles.DrawAAConvexPolygon(p1Min, p1Max, p2Max, p2Min);
                }

                // Создаем верхнюю и нижнюю крышки через фанатную триангуляцию (для выпуклых форм)
                Vector3[] topPoints = new Vector3[border.PointCount];
                Vector3[] bottomPoints = new Vector3[border.PointCount];
                for (int i = 0; i < border.PointCount; i++)
                {
                    topPoints[i] = ToWorld(border, border.GetPoint(i), border.MaxY);
                    bottomPoints[i] = ToWorld(border, border.GetPoint(i), border.MinY);
                }
                Handles.DrawAAConvexPolygon(topPoints);
                Handles.DrawAAConvexPolygon(bottomPoints);
            }

            // ---- Отрисовка контуров ----
            Handles.color = ShapeEditorGUIUtility.OutlineColor;
            DrawLoopHandles(border, border.MinY);
            DrawLoopHandles(border, border.MaxY);

            if (_selected.Count > 0)
            {
                Handles.color = ShapeEditorGUIUtility.SelectedColor;
                foreach (int i in _selected)
                {
                    if (i < 0 || i >= border.PointCount) continue;
                    Vector3 p = border.GetPoint(i);
                    Vector3 a = ToWorld(border, p, border.MinY);
                    Vector3 b = ToWorld(border, p, border.MaxY);
                    Handles.DrawAAPolyLine(3f, a, b);
                }
            }

            Handles.color = prev;
        }

        private void DrawLoopHandles(ShapeBorder border, float y)
        {
            for (int i = 0; i < border.PointCount - 1; i++)
            {
                Vector3 a = ToWorld(border, border.GetPoint(i), y);
                Vector3 b = ToWorld(border, border.GetPoint(i + 1), y);
                Handles.DrawAAPolyLine(2f, a, b);
            }

            if (border.Closed && border.PointCount > 2)
            {
                Vector3 a = ToWorld(border, border.GetPoint(border.PointCount - 1), y);
                Vector3 b = ToWorld(border, border.GetPoint(0), y);
                Handles.DrawAAPolyLine(2f, a, b);
            }
        }

        private static Vector3 ToWorld(ShapeBorder border, Vector3 local, float y)
        {
            return border.transform.TransformPoint(new Vector3(local.x, y, local.z));
        }

        protected override void DrawShapeHandles()
        {
            var border = (ShapeBorder)target;
            if (border.PointCount == 0) return;

            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlId);

            // ---- 1. Вершины ----
            for (int i = 0; i < border.PointCount; i++)
            {
                Vector3 local = border.GetPoint(i);
                Vector3 world = ToWorld(border, local, border.MinY);

                float size = HandleUtility.GetHandleSize(world) * HANDLE_SIZE_SCALE;
                bool isSelected = _selected.Contains(i);

                Handles.color = isSelected ? ShapeEditorGUIUtility.SelectedColor : ShapeEditorGUIUtility.HandleColor;

                if (Handles.Button(world, Quaternion.identity, size, size * 1.4f, Handles.SphereHandleCap))
                {
                    bool additive = Event.current.shift || Event.current.control;
                    if (!additive) _selected.Clear();
                    if (!_selected.Add(i)) _selected.Remove(i);

                    Event.current.Use();
                    SceneView.RepaintAll();
                }
            }

            // ---- 2. Добавление вершин посередине ----
            int segmentCount = border.Closed ? border.PointCount : border.PointCount - 1;
            int insertIndex = -1;
            Vector3 insertValue = Vector3.zero;

            for (int i = 0; i < segmentCount; i++)
            {
  
                int next = (i + 1) % border.PointCount;
                Vector3 a = border.GetPoint(i);
                Vector3 b = border.GetPoint(next);
                Vector3 midLocal = (a + b) * 0.5f;
                Vector3 midWorld = ToWorld(border, midLocal, border.MinY);
                float size = HandleUtility.GetHandleSize(midWorld) * HANDLE_SIZE_SCALE;
                Handles.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
                if (Handles.Button(midWorld, Quaternion.identity, size, size * 2f, Handles.DotHandleCap))
                {
                    insertIndex = next;
                    insertValue = midLocal;
                    Event.current.Use();
                    break;
                }
            }
            if (insertIndex != -1)
            {
                Undo.RecordObject(border, "Insert ShapeBorder Vertex");
                border.InsertPoint(insertIndex, insertValue);
                EditorUtility.SetDirty(border);
                _selected.Clear();
                _selected.Add(insertIndex);
                SceneView.RepaintAll();
                return;
            }
            // ---- 3. Перемещение вершин ----
            if (_selected.Count > 0)
            {
                Vector3 pivotLocal = Vector3.zero;
                int validCount = 0;
                foreach (int idx in _selected)
                {
                    if (idx < 0 || idx >= border.PointCount) continue;
                    pivotLocal += border.GetPoint(idx);
                    validCount++;
                }
                if (validCount > 0)
                {
                    pivotLocal /= validCount;
                    Vector3 pivotWorld = ToWorld(border, pivotLocal, border.MinY);
                    EditorGUI.BeginChangeCheck();
                    Vector3 newPivotWorld = Handles.PositionHandle(pivotWorld, border.transform.rotation);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Vector3 deltaWorld = newPivotWorld - pivotWorld;
                        Vector3 deltaLocal = border.transform.InverseTransformVector(deltaWorld);
                        deltaLocal.y = 0f;
                        Undo.RecordObject(border, "Move ShapeBorder Vertex");
                        var indices = new List<int>(_selected);
                        indices.Sort();
                        for (int k = 0; k < indices.Count; k++)
                        {
                            int idx = indices[k];
                            if (idx < 0 || idx >= border.PointCount) continue;
                            Vector3 p = border.GetPoint(idx) + deltaLocal;
                            p.y = 0f;
                            border.SetPoint(idx, p);
                        }
                        EditorUtility.SetDirty(border);
                    }
                }
            }
            // ---- 4. Высота плиты (MinY / MaxY) ----
            Vector3 anchor = border.transform.position;
            DrawSlabHandle(border, anchor, border.MinY, true);
            DrawSlabHandle(border, anchor, border.MaxY, false);
            // ---- 5. Снятие выделения ----
            if (Event.current.type == EventType.MouseDown
            && Event.current.button == 0
            && !Event.current.alt && !Event.current.shift && !Event.current.control
            && GUIUtility.hotControl == 0)
            {
                _selected.Clear();
                SceneView.RepaintAll();
            }
            // ---- 6. Контекстное меню ----
            if (Event.current.type == EventType.ContextClick && _selected.Count == 1)
            {
                int idx = -1;
                foreach (int s in _selected) idx = s;
                if (idx >= 0 && idx < border.PointCount)
                {
                    Vector3 world = ToWorld(border, border.GetPoint(idx), border.MinY);
                    float dist = HandleUtility.DistanceToCircle(world, HandleUtility.GetHandleSize(world) * HANDLE_SIZE_SCALE);
                    if (dist < 8f)
                    {
                        ShowPointContextMenu(idx);
                        Event.current.Use();
                    }
                }
            }
        }
        private void DrawSlabHandle(ShapeBorder border, Vector3 anchorWorld, float y, bool isMin)
        {
            Vector3 world = anchorWorld + border.transform.up * y;
            float size = HandleUtility.GetHandleSize(world) * HANDLE_SIZE_SCALE;
            Handles.color = isMin ? new Color(0.9f, 0.4f, 0.4f) : new Color(0.4f, 0.7f, 0.9f);
            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.Slider(world, border.transform.up, size, Handles.CubeHandleCap, 0.01f);
            if (EditorGUI.EndChangeCheck())
            {
                float newY = border.transform.InverseTransformPoint(moved).y;
                Undo.RecordObject(border, isMin ? "Edit ShapeBorder MinY" : "Edit ShapeBorder MaxY");
                if (isMin)
                {
                    border.MinY = newY;
                    if (border.MaxY < newY) border.MaxY = newY;
                }
                else
                {
                    border.MaxY = newY;
                    if (border.MinY > newY) border.MinY = newY;
                }
                EditorUtility.SetDirty(border);
            }
            Handles.Label(world + border.transform.right * size * 1.5f, isMin ? "MinY" : "MaxY");
        }
        private void ShowPointContextMenu(int index)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Insert Point After"), false, () =>
            {
                var border = (ShapeBorder)target;
                if (border.PointCount < 1) return;
                Undo.RecordObject(border, "Insert ShapeBorder Vertex");
                Vector3 a = border.GetPoint(index);
                Vector3 b = border.GetPoint((index + 1) % border.PointCount);
                border.InsertPoint(index + 1, (a + b) * 0.5f);
                EditorUtility.SetDirty(border);
            });
            menu.AddItem(new GUIContent("Delete Point"), false, () =>
            {
                var border = (ShapeBorder)target;
                Undo.RecordObject(border, "Delete ShapeBorder Vertex");
                border.RemovePoint(index);
                _selected.Clear();
                EditorUtility.SetDirty(border);
                SceneView.RepaintAll();
            });
            menu.ShowAsContext();
        }
    }
}