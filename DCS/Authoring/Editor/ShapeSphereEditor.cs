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
            float worldRadius = sphere.Radius * Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));

            var prev = Handles.color;
            Handles.color = ShapeEditorGUIUtility.FillColor;
            Handles.DrawSolidDisc(sphere.transform.position, Vector3.up, worldRadius * 0.0001f); // cheap fill substitute
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
            float worldRadius = sphere.Radius * Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
            Vector3 center = sphere.transform.position;

            // Three radial handles along world axes.
            Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };

            EditorGUI.BeginChangeCheck();
            float newRadius = sphere.Radius;

            for (int i = 0; i < axes.Length; i++)
            {
                Vector3 dir = axes[i];
                Vector3 handlePos = center + dir * worldRadius;
                float size = HandleUtility.GetHandleSize(handlePos) * 0.12f;

                Handles.color = ShapeEditorGUIUtility.HandleColor;
                Vector3 moved = Handles.Slider(handlePos, dir, size, Handles.SphereHandleCap, 0.01f);

                if (moved != handlePos)
                {
                    float newWorldRadius = Vector3.Distance(center, moved);
                    float localScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
                    newRadius = Mathf.Max(MinRadius, newWorldRadius / Mathf.Max(localScale, 0.0001f));
                }
            }

            // Uniform centre handle.
            Handles.color = ShapeEditorGUIUtility.SelectedColor;
            float centerHandleSize = HandleUtility.GetHandleSize(center) * 0.18f;
            var fmh_89_25_639262227192589225 = Quaternion.identity; Vector3 uniform = Handles.FreeMoveHandle(
                center, centerHandleSize, Vector3.one * 0.05f, Handles.CircleHandleCap);

            if (uniform != center)
            {
                float newWorldRadius = Vector3.Distance(center, uniform);
                float localScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
                newRadius = Mathf.Max(MinRadius, newWorldRadius / Mathf.Max(localScale, 0.0001f));
            }

            if (EditorGUI.EndChangeCheck())
            {
                RecordSerializedUndo("Resize ShapeSphere");
                _radiusProp.floatValue = newRadius;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}