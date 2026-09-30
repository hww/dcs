using UnityEditor;
using UnityEngine;

namespace DCS.Authoring.Editor
{
    [CustomEditor(typeof(ShapeCylinder))]
    [CanEditMultipleObjects]
    public sealed class ShapeCylinderEditor : BaseShapeEditor
    {
        private SerializedProperty _radiusProp;
        private SerializedProperty _heightProp;

        private const float MinRadius = 0.01f;
        private const float MinHeight = 0.01f;

        protected override void OnEnable()
        {
            base.OnEnable();
            _radiusProp = serializedObject.FindProperty("_radius");
            _heightProp = serializedObject.FindProperty("_height");
        }

        protected override string TargetDisplayName => "Shape Cylinder";

        protected override void DrawShapeInspector()
        {
            EditorGUILayout.LabelField("Dimensions", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            float r = EditorGUILayout.FloatField("Radius", _radiusProp.floatValue);
            float h = EditorGUILayout.FloatField("Height", _heightProp.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                _radiusProp.floatValue = Mathf.Max(MinRadius, r);
                _heightProp.floatValue = Mathf.Max(MinHeight, h);
            }

            EditorGUI.indentLevel--;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset"))
                {
                    _radiusProp.floatValue = 1f;
                    _heightProp.floatValue = 2f;
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        protected override void DrawShapeSceneGizmos()
        {
            var cyl = (ShapeCylinder)target;
            Vector3 ls = cyl.transform.lossyScale;
            float rScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.z));
            float hScale = Mathf.Abs(ls.y);

            float r = cyl.Radius * rScale;
            float h = cyl.Height * hScale;
            float halfH = h * 0.5f;

            Vector3 up = cyl.transform.up;
            Vector3 pos = cyl.transform.position;

            var prev = Handles.color;
            Matrix4x4 prevMatrix = Handles.matrix;

            // ---- ИСПРАВЛЕННАЯ ЗАЛИВКА ЦИЛИНДРА: деление высоты на 2 ----
            Handles.color = ShapeEditorGUIUtility.FillColor;

            // Встроенный CylinderHandleCap ориентирован вдоль локальной оси Z.
            // LookRotation разворачивает эту локальную ось Z строго по вектору Transform.up компонента.
            Quaternion correctLook = Quaternion.LookRotation(up, cyl.transform.forward);

            // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Встроенный примитив Unity имеет высоту 2.0. 
            // Чтобы он совпал с контурами, по оси Z матрицы передаем h * 0.5f. По осям X и Y — диаметры (r * 2).
            Vector3 correctScale = new Vector3(r * 2f, r * 2f, h);

            Handles.matrix = Matrix4x4.TRS(pos, correctLook, correctScale);
            Handles.CylinderHandleCap(0, Vector3.zero, Quaternion.identity, 1f, EventType.Repaint);

            // Сбрасываем матрицу обратно для отрисовки тонких линий контура
            Handles.matrix = prevMatrix;

            // ---- КОНТУРНЫЕ ЛИНИИ ----
            Handles.color = ShapeEditorGUIUtility.OutlineColor;
            Handles.DrawWireDisc(pos + up * halfH, up, r);
            Handles.DrawWireDisc(pos - up * halfH, up, r);

            Vector3 right = cyl.transform.right;
            Vector3 forward = cyl.transform.forward;
            for (int i = 0; i < 4; i++)
            {
                Vector3 dir = Quaternion.AngleAxis(i * 90f, up) * right;
                Vector3 a = pos - up * halfH + dir * r;
                Vector3 b = pos + up * halfH + dir * r;
                Handles.DrawLine(a, b);
            }

            Handles.color = prev;
        }

        protected override void DrawShapeHandles()
        {
            var cyl = (ShapeCylinder)target;
            Vector3 ls = cyl.transform.lossyScale;
            float rScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.z));
            float hScale = Mathf.Abs(ls.y);

            float r = cyl.Radius * rScale;
            float h = cyl.Height * hScale;
            float halfH = h * 0.5f;

            Vector3 up = cyl.transform.up;
            Vector3 pos = cyl.transform.position;

            EditorGUI.BeginChangeCheck();
            float newRadius = cyl.Radius;
            float newHeight = cyl.Height;

            // --- Height handle (top cap) ---
            Vector3 topHandle = pos + up * halfH;
            float topSize = HandleUtility.GetHandleSize(topHandle) * HANDLE_SIZE_SCALE;

            Handles.color = ShapeEditorGUIUtility.HandleColor;
            Vector3 movedTop = Handles.Slider(topHandle, up, topSize, Handles.CubeHandleCap, 0.01f);
            if (movedTop != topHandle)
            {
                float newH = Vector3.Dot(movedTop - pos, up) * 2f;
                newHeight = Mathf.Max(MinHeight, newH / Mathf.Max(hScale, 0.0001f));
            }

            // --- Bottom cap ---
            Vector3 bottomHandle = pos - up * halfH;
            float bottomSize = HandleUtility.GetHandleSize(bottomHandle) * HANDLE_SIZE_SCALE;
            Vector3 movedBottom = Handles.Slider(bottomHandle, -up, bottomSize, Handles.CubeHandleCap, 0.01f);
            if (movedBottom != bottomHandle)
            {
                float newH = Vector3.Dot(pos - movedBottom, up) * 2f;
                newHeight = Mathf.Max(MinHeight, newH / Mathf.Max(hScale, 0.0001f));
            }

            // --- Radius handle ---
            Vector3 radiusDir = cyl.transform.right;
            Vector3 radiusHandle = pos + radiusDir * r;
            float radiusSize = HandleUtility.GetHandleSize(radiusHandle) * HANDLE_SIZE_SCALE;

            Handles.color = ShapeEditorGUIUtility.SelectedColor;
            Vector3 movedRadius = Handles.Slider(radiusHandle, radiusDir, radiusSize, Handles.SphereHandleCap, 0.01f);
            if (movedRadius != radiusHandle)
            {
                float newR = Vector3.Distance(pos, movedRadius);
                newRadius = Mathf.Max(MinRadius, newR / Mathf.Max(rScale, 0.0001f));
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(cyl, "Resize ShapeCylinder");
                cyl.Radius = newRadius;
                cyl.Height = newHeight;
                EditorUtility.SetDirty(cyl);
            }
        }
    }
}
