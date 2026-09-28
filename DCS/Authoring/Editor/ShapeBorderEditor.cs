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

        private int _selectedPoint = -1;
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
            // ---- Slab -----------------------------------------------------
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

            // ---- Closed flag ----------------------------------------------
            EditorGUI.BeginChangeCheck();
            bool closed = EditorGUILayout.ToggleLeft("Closed Loop", _closedProp.boolValue);
            if (EditorGUI.EndChangeCheck())
                _closedProp.boolValue = closed;

            // ---- Points ---------------------------------------------------
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
                        GUI.color = (_selectedPoint == i) ? ShapeEditorGUIUtility.SelectedColor : Color.white;

                        if (GUILayout.Button($"{i}", EditorStyles.miniButton, GUILayout.Width(28)))
                            _selectedPoint = (_selectedPoint == i) ? -1 : i;

                        GUI.color = prevColor;

                        EditorGUI.BeginChangeCheck();
                        Vector3 v = EditorGUILayout.Vector3Field(GUIContent.none, element.vector3Value);
                        if (EditorGUI.EndChangeCheck())
                            element.vector3Value = v;

                        if (GUILayout.Button("\u2715", EditorStyles.miniButton, GUILayout.Width(22)))
                        {
                            _pointsProp.DeleteArrayElementAtIndex(i);
                            if (_selectedPoint >= _pointsProp.arraySize)
                                _selectedPoint = -1;
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

        // ---------------------------------------------------------------
        // Scene view
        // ---------------------------------------------------------------

        protected override void DrawShapeSceneGizmos()
        {
            var border = (ShapeBorder)target;
            if (border.PointCount < 2) return;

            var prev = Handles.color;

            // Slab outline (translucent fill substitute: just thick lines).
            Handles.color = ShapeEditorGUIUtility.OutlineColor;
            DrawLoopHandles(border, border.MinY);
            DrawLoopHandles(border, border.MaxY);

            // Vertical struts on selected point only to keep the view clean.
            if (_selectedPoint >= 0 && _selectedPoint < border.PointCount)
            {
                Vector3 a = border.transform.TransformPoint(
                    new Vector3(border.GetPoint(_selectedPoint).x, border.MinY, border.GetPoint(_selectedPoint).z));
                Vector3 b = border.transform.TransformPoint(
                    new Vector3(border.GetPoint(_selectedPoint).x, border.MaxY, border.GetPoint(_selectedPoint).z));
                Handles.color = ShapeEditorGUIUtility.SelectedColor;
                Handles.DrawAAPolyLine(3f, a, b);
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

            float yMid = (border.MinY + border.MaxY) * 0.5f;

            // ---- Per-vertex handles ---------------------------------------
            for (int i = 0; i < border.PointCount; i++)
            {
                Vector3 local = border.GetPoint(i);
                Vector3 world = border.transform.TransformPoint(new Vector3(local.x, yMid, local.z));

                float size = HandleUtility.GetHandleSize(world) * 0.10f;
                bool isSelected = (_selectedPoint == i);

                Handles.color = isSelected ? ShapeEditorGUIUtility.SelectedColor : ShapeEditorGUIUtility.HandleColor;

                EditorGUI.BeginChangeCheck();
                var fmh_188_28_639262228065610573 = Quaternion.identity; Vector3 moved = Handles.FreeMoveHandle(
                    world, size, Vector3.one * 0.01f, Handles.SphereHandleCap);

                if (EditorGUI.EndChangeCheck())
                {
                    // Convert back to local space and project onto X/Z plane.
                    Vector3 localMoved = border.transform.InverseTransformPoint(moved);
                    localMoved.y = 0f;

                    RecordSerializedUndo("Move ShapeBorder Vertex");
                    _pointsProp.GetArrayElementAtIndex(i).vector3Value = localMoved;
                    serializedObject.ApplyModifiedProperties();
                    _selectedPoint = i;
                }

                // Alt-click to delete.
                if (Event.current.type == EventType.MouseDown
                    && Event.current.alt
                    && HandleUtility.nearestControl == GUIUtility.GetControlID(FocusType.Passive)
                    && isSelected)
                {
                    // The FreeMoveHandle already consumed the click; use the
                    // context-menu route instead.
                }

                // Right-click a vertex -> context menu.
                if (isSelected && Event.current.type == EventType.ContextClick)
                {
                    ShowPointContextMenu(i);
                    Event.current.Use();
                }
            }

            // ---- Segment midpoint handles (insert) ------------------------
            int segmentCount = border.Closed ? border.PointCount : border.PointCount - 1;
            for (int i = 0; i < segmentCount; i++)
            {
                int next = (i + 1) % border.PointCount;
                Vector3 a = border.GetPoint(i);
                Vector3 b = border.GetPoint(next);
                Vector3 midLocal = (a + b) * 0.5f;
                Vector3 midWorld = border.transform.TransformPoint(new Vector3(midLocal.x, yMid, midLocal.z));

                float size = HandleUtility.GetHandleSize(midWorld) * 0.06f;
                Handles.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);

                if (Handles.Button(midWorld, Quaternion.identity, size, size, Handles.DotHandleCap))
                {
                    Undo.RecordObject(target, "Insert ShapeBorder Vertex");
                    _pointsProp.InsertArrayElementAtIndex(next);
                    _pointsProp.GetArrayElementAtIndex(next).vector3Value = midLocal;
                    serializedObject.ApplyModifiedProperties();
                    _selectedPoint = next;
                    break;
                }
            }

            // ---- Slab (MinY / MaxY) vertical handles ----------------------
            // Two horizontal handle bars, one per slab plane, offset from the
            // centroid so they don't overlap with vertex handles.
            Vector3 centroid = ComputeCentroid(border);
            Vector3 centroidLocal = border.transform.InverseTransformPoint(centroid);
            centroidLocal.y = 0f;
            Vector3 centroidWorld = border.transform.TransformPoint(centroidLocal);

            DrawSlabHandle(border, centroidWorld, border.MinY, true);
            DrawSlabHandle(border, centroidWorld, border.MaxY, false);
        }

        private void DrawSlabHandle(ShapeBorder border, Vector3 anchorWorld, float y, bool isMin)
        {
            Vector3 world = anchorWorld + border.transform.up * y;
            // Slight offset so the handle doesn't sit exactly on the wire loop.
            world += border.transform.right * HandleUtility.GetHandleSize(world) * 0.2f;

            float size = HandleUtility.GetHandleSize(world) * 0.12f;
            Handles.color = isMin
                ? new Color(0.9f, 0.4f, 0.4f)
                : new Color(0.4f, 0.7f, 0.9f);

            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.Slider(world, border.transform.up, size, Handles.CubeHandleCap, 0.01f);
            if (EditorGUI.EndChangeCheck())
            {
                float newY = border.transform.InverseTransformPoint(moved).y;
                RecordSerializedUndo(isMin ? "Edit ShapeBorder MinY" : "Edit ShapeBorder MaxY");

                if (isMin)
                {
                    _minYProp.floatValue = newY;
                    if (_maxYProp.floatValue < newY)
                        _maxYProp.floatValue = newY;
                }
                else
                {
                    _maxYProp.floatValue = newY;
                    if (_minYProp.floatValue > newY)
                        _minYProp.floatValue = newY;
                }

                serializedObject.ApplyModifiedProperties();
            }

            Handles.Label(world, isMin ? "MinY" : "MaxY");
        }

        private static Vector3 ComputeCentroid(ShapeBorder border)
        {
            if (border.PointCount == 0) return border.transform.position;
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < border.PointCount; i++)
                sum += border.GetPoint(i);
            return border.transform.TransformPoint(sum / border.PointCount);
        }

        private void ShowPointContextMenu(int index)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Insert Point After"), false, () =>
            {
                Undo.RecordObject(target, "Insert ShapeBorder Vertex");
                var border = (ShapeBorder)target;
                Vector3 a = border.GetPoint(index);
                Vector3 b = border.GetPoint((index + 1) % border.PointCount);
                _pointsProp.InsertArrayElementAtIndex(index + 1);
                _pointsProp.GetArrayElementAtIndex(index + 1).vector3Value = (a + b) * 0.5f;
                serializedObject.ApplyModifiedProperties();
            });

            menu.AddItem(new GUIContent("Delete Point"), false, () =>
            {
                Undo.RecordObject(target, "Delete ShapeBorder Vertex");
                _pointsProp.DeleteArrayElementAtIndex(index);
                _selectedPoint = -1;
                serializedObject.ApplyModifiedProperties();
            });

            menu.ShowAsContext();
        }
    }
}