#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace DynamicComponent.Editor
{
    [CustomEditor(typeof(ZoneBorder))]
    public class ZoneBorderEditor : UnityEditor.Editor
    {
        private ZoneBorder _border;

        private int _selectedPoint = -1;

        private const float HandleSize = 0.08f;
        private const float SelectedHandleSize = 0.12f;

        private void OnEnable()
        {
            _border = (ZoneBorder)target;

            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField(
                "Point Editing",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "Points",
                _border.PointCount.ToString());

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = _selectedPoint >= 0;

                if (GUILayout.Button("Add Before"))
                {
                    AddPointBefore();
                }

                if (GUILayout.Button("Add After"))
                {
                    AddPointAfter();
                }

                GUI.enabled = true;
            }

            GUI.enabled = _selectedPoint >= 0;

            if (GUILayout.Button("Delete Selected Point"))
            {
                DeleteSelectedPoint();
            }

            GUI.enabled = true;

            EditorGUILayout.Space(4);

            if (GUILayout.Button("Add Point At End"))
            {
                AddPointAtEnd();
            }

            if (GUILayout.Button("Select First Point"))
            {
                SelectPoint(0);
            }

            if (GUILayout.Button("Clear Selection"))
            {
                ClearSelection();
            }

            if (GUILayout.Button("Flaten"))
            {
                SetY(0);
            }

            EditorGUILayout.Space(8);

            if (_selectedPoint >= 0 &&
                _selectedPoint < _border.PointCount)
            {
                EditorGUILayout.HelpBox(
                    $"Selected point: {_selectedPoint}",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Click a point in the Scene View to select it.",
                    MessageType.None);
            }

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField(
                "Scene shortcuts",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "A",
                "Add point after selected");

            EditorGUILayout.LabelField(
                "Shift + A",
                "Add point before selected");

            EditorGUILayout.LabelField(
                "Delete",
                "Delete selected point");

            EditorGUILayout.LabelField(
                "[ / ]",
                "Select previous / next point");

            EditorGUILayout.LabelField(
                "C",
                "Toggle closed / open");

            EditorGUILayout.LabelField(
                "Escape",
                "Clear selection");
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_border == null)
                return;

            DrawLines();
            DrawPointHandles();
            HandleKeyboard(sceneView);
        }

        private void DrawLines()
        {
            if (_border.Points == null ||
                _border.Points.Length < 2)
            {
                return;
            }

            Handles.color = Color.cyan;

            for (int i = 0;
                     i < _border.Points.Length - 1;
                     i++)
            {
                Vector3 a =
                    _border.transform.TransformPoint(
                        _border.Points[i]);

                Vector3 b =
                    _border.transform.TransformPoint(
                        _border.Points[i + 1]);

                Handles.DrawLine(a, b, 2f);
            }

            if (_border.Closed &&
                _border.Points.Length >= 3)
            {
                Vector3 last =
                    _border.transform.TransformPoint(
                        _border.Points[
                            _border.Points.Length - 1]);

                Vector3 first =
                    _border.transform.TransformPoint(
                        _border.Points[0]);

                Handles.DrawLine(
                    last,
                    first,
                    2f);
            }
        }

        private void DrawPointHandles()
        {
            if (_border.Points == null)
                return;

            for (int i = 0;
                     i < _border.Points.Length;
                     i++)
            {
                DrawPointHandle(i);
            }
        }

        private void DrawPointHandle(int index)
        {
            Vector3 localPosition =
                _border.Points[index];

            Vector3 worldPosition =
                _border.transform.TransformPoint(
                    localPosition);

            float size =
                HandleUtility.GetHandleSize(
                    worldPosition);

            bool selected =
                index == _selectedPoint;

            float handleSize =
                size *
                (selected
                    ? SelectedHandleSize
                    : HandleSize);

            Handles.color =
                selected
                    ? Color.yellow
                    : Color.cyan;

            if (Handles.Button(
                    worldPosition,
                    Quaternion.identity,
                    handleSize,
                    handleSize,
                    Handles.DotHandleCap))
            {
                SelectPoint(index);
            }

            if (!selected)
                return;

            EditorGUI.BeginChangeCheck();

            Vector3 newWorldPosition =
                Handles.PositionHandle(
                    worldPosition,
                    _border.transform.rotation);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(
                    _border,
                    "Move Zone Border Point");

                Vector3 newLocalPosition =
                    _border.transform.InverseTransformPoint(
                        newWorldPosition);

                _border.Points[index] =
                    newLocalPosition;

                EditorUtility.SetDirty(_border);
            }
        }

        private void HandleKeyboard(SceneView sceneView)
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.KeyDown)
                return;

            if (GUIUtility.hotControl != 0)
                return;

            switch (currentEvent.keyCode)
            {
                case KeyCode.A:

                    if (currentEvent.shift)
                        AddPointBefore();
                    else
                        AddPointAfter();

                    currentEvent.Use();
                    break;

                case KeyCode.Delete:
                case KeyCode.Backspace:

                    DeleteSelectedPoint();

                    currentEvent.Use();
                    break;

                case KeyCode.LeftBracket:

                    SelectPreviousPoint();

                    currentEvent.Use();
                    break;

                case KeyCode.RightBracket:

                    SelectNextPoint();

                    currentEvent.Use();
                    break;

                case KeyCode.C:

                    ToggleClosed();

                    currentEvent.Use();
                    break;

                case KeyCode.Escape:

                    ClearSelection();

                    currentEvent.Use();
                    break;
            }
        }

        private void SelectPoint(int index)
        {
            if (_border.Points == null)
                return;

            if (index < 0 ||
                index >= _border.Points.Length)
            {
                return;
            }

            _selectedPoint = index;

            Selection.activeObject = _border;

            SceneView.RepaintAll();
            Repaint();
        }

        private void SelectPreviousPoint()
        {
            if (_border.PointCount == 0)
                return;

            if (_selectedPoint <= 0)
                SelectPoint(
                    _border.PointCount - 1);
            else
                SelectPoint(
                    _selectedPoint - 1);
        }

        private void SelectNextPoint()
        {
            if (_border.PointCount == 0)
                return;

            if (_selectedPoint < 0 ||
                _selectedPoint >= _border.PointCount - 1)
            {
                SelectPoint(0);
            }
            else
            {
                SelectPoint(
                    _selectedPoint + 1);
            }
        }

        private void ClearSelection()
        {
            _selectedPoint = -1;

            SceneView.RepaintAll();
            Repaint();
        }

        private void AddPointBefore()
        {
            if (_selectedPoint < 0)
            {
                AddPointAtEnd();
                return;
            }

            InsertPoint(_selectedPoint);
        }

        private void AddPointAfter()
        {
            if (_selectedPoint < 0)
            {
                AddPointAtEnd();
                return;
            }

            InsertPoint(_selectedPoint + 1);
        }

        private void AddPointAtEnd()
        {
            int count =
                _border.Points != null
                    ? _border.Points.Length
                    : 0;

            InsertPoint(count);
        }

        private void InsertPoint(int insertIndex)
        {
            Vector3 newPoint =
                CalculateInsertedPoint(insertIndex);

            Undo.RecordObject(
                _border,
                "Add Zone Border Point");

            Vector3[] oldPoints =
                _border.Points ??
                new Vector3[0];

            Vector3[] newPoints =
                new Vector3[oldPoints.Length + 1];

            for (int i = 0, j = 0;
                 i < newPoints.Length;
                 i++)
            {
                if (i == insertIndex)
                {
                    newPoints[i] = newPoint;
                    continue;
                }

                newPoints[i] = oldPoints[j];
                j++;
            }

            _border.Points = newPoints;

            _selectedPoint = insertIndex;

            EditorUtility.SetDirty(_border);

            SceneView.RepaintAll();
            Repaint();
        }

        private Vector3 CalculateInsertedPoint(
            int insertIndex)
        {
            if (_border.Points == null ||
                _border.Points.Length == 0)
            {
                return Vector3.zero;
            }

            int count =
                _border.Points.Length;

            if (count == 1)
            {
                return _border.Points[0] +
                       Vector3.forward;
            }

            if (insertIndex <= 0)
            {
                Vector3 first =
                    _border.Points[0];

                Vector3 second =
                    _border.Points[1];

                return first +
                       (first - second) * 0.5f;
            }

            if (insertIndex >= count)
            {
                Vector3 last =
                    _border.Points[count - 1];

                Vector3 previous =
                    _border.Points[count - 2];

                return last +
                       (last - previous) * 0.5f;
            }

            Vector3 a =
                _border.Points[insertIndex - 1];

            Vector3 b =
                _border.Points[insertIndex];

            return Vector3.Lerp(a, b, 0.5f);
        }

        private void DeleteSelectedPoint()
        {
            if (_selectedPoint < 0 ||
                _border.Points == null ||
                _selectedPoint >= _border.Points.Length)
            {
                return;
            }

            Undo.RecordObject(
                _border,
                "Delete Zone Border Point");

            Vector3[] oldPoints =
                _border.Points;

            Vector3[] newPoints =
                new Vector3[oldPoints.Length - 1];

            for (int i = 0, j = 0;
                 i < oldPoints.Length;
                 i++)
            {
                if (i == _selectedPoint)
                    continue;

                newPoints[j] = oldPoints[i];
                j++;
            }

            _border.Points = newPoints;

            if (newPoints.Length == 0)
            {
                _selectedPoint = -1;
            }
            else if (_selectedPoint >= newPoints.Length)
            {
                _selectedPoint =
                    newPoints.Length - 1;
            }

            EditorUtility.SetDirty(_border);

            SceneView.RepaintAll();
            Repaint();
        }

        private void ToggleClosed()
        {
            Undo.RecordObject(
                _border,
                "Toggle Zone Border Closed");

            _border.Closed =
                !_border.Closed;

            EditorUtility.SetDirty(_border);

            SceneView.RepaintAll();
            Repaint();
        }

        private void Flaten()
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0, j = 0;
                            i < _border.PointCount;
                            i++)
            {
                var point = _border.GetPoint(j);
                if (point.y > max) max = point.y;
                if (point.y < min) min = point.y;
                j++;
            }
            var average = (min + max) * 0.5f;
            for (int i = 0, j = 0;
                            i < _border.PointCount;
                            i++)
            {
                var point = _border.GetPoint(j);
                point.y = average;
                _border.SetPoint(j,point);
                j++;
            }
        }


        private void SetY(float value)
        {
            for (int i = 0, j = 0;
                            i < _border.PointCount;
                            i++)
            {
                var point = _border.GetPoint(j);
                point.y = value;
                _border.SetPoint(j, point);
                j++;
            }
        }
    }
}

#endif

