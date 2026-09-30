using UnityEditor;
using UnityEngine;

namespace DCS.Authoring.Editor
{
    [CustomEditor(typeof(ShapeSphere))]
    [CanEditMultipleObjects]
    public sealed class ShapeSphereEditor : BaseShapeEditor
    {
        private SerializedProperty _radiusProp;
        private const float MinRadius = 0.01f;

        protected override void OnEnable()
        {
            base.OnEnable();
            _radiusProp = serializedObject.FindProperty("_radius");
        }

        protected override string TargetDisplayName => "Shape Sphere";

        protected override void DrawShapeInspector()
        {
            EditorGUILayout.LabelField("Dimensions", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            float r = EditorGUILayout.FloatField("Radius", _radiusProp.floatValue);
            if (EditorGUI.EndChangeCheck())
                _radiusProp.floatValue = Mathf.Max(MinRadius, r);

            EditorGUI.indentLevel--;

            var sphere = (ShapeSphere)target;
            Vector3 ls = sphere.transform.lossyScale;
            float worldRadius = sphere.Radius * Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
            EditorGUILayout.LabelField("World Radius", worldRadius.ToString("0.###"), EditorStyles.miniLabel);
        }

        protected override void DrawShapeSceneGizmos()
        {
            var sphere = (ShapeSphere)target;
            Vector3 ls = sphere.transform.lossyScale;

            // Вычисляем финальный радиус с учетом максимальной оси масштаба (как в самом компоненте)
            float maxScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y));
            maxScale = Mathf.Max(maxScale, Mathf.Abs(ls.z));
            float worldRadius = sphere.Radius * maxScale;

            var prev = Handles.color;
            Matrix4x4 prevMatrix = Handles.matrix;

            // ---- ИСПРАВЛЕННАЯ ЗАЛИВКА СФЕРЫ ----
            Handles.color = ShapeEditorGUIUtility.FillColor;
            // Устанавливаем матрицу с точным мировым размером сферы во все стороны (равномерный объем)
            Handles.matrix = Matrix4x4.TRS(sphere.transform.position, Quaternion.identity, Vector3.one * worldRadius * 2f);
            Handles.SphereHandleCap(0, Vector3.zero, Quaternion.identity, 1f, EventType.Repaint);

            // Восстанавливаем матрицу для дисков контура
            Handles.matrix = prevMatrix;

            // ---- КОНТУР ----
            Handles.color = ShapeEditorGUIUtility.OutlineColor;
            Handles.DrawWireDisc(sphere.transform.position, Vector3.up, worldRadius);
            Handles.DrawWireDisc(sphere.transform.position, Vector3.right, worldRadius);
            Handles.DrawWireDisc(sphere.transform.position, Vector3.forward, worldRadius);

            Handles.color = prev;
        }

        protected override void DrawShapeHandles()
        {
            var sphere = (ShapeSphere)target;
            Vector3 ls = sphere.transform.lossyScale;

            float maxScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y));
            maxScale = Mathf.Max(maxScale, Mathf.Abs(ls.z));
            float worldRadius = sphere.Radius * maxScale;

            Vector3 center = sphere.transform.position;
            Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };

            EditorGUI.BeginChangeCheck();
            float newRadius = sphere.Radius;

            for (int i = 0; i < axes.Length; i++)
            {
                Vector3 dir = axes[i];
                Vector3 handlePos = center + dir * worldRadius;
                float size = HandleUtility.GetHandleSize(handlePos) * HANDLE_SIZE_SCALE;

                Handles.color = ShapeEditorGUIUtility.HandleColor;
                Vector3 moved = Handles.Slider(handlePos, dir, size, Handles.SphereHandleCap, 0.01f);

                if (moved != handlePos)
                {
                    float newWorldRadius = Vector3.Distance(center, moved);
                    newRadius = Mathf.Max(MinRadius, newWorldRadius / Mathf.Max(maxScale, 0.0001f));
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sphere, "Resize ShapeSphere");
                sphere.Radius = newRadius;
                EditorUtility.SetDirty(sphere);
            }
        }
    }
}
