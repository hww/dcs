#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace DynamicComponent.Editor
{
    [CustomEditor(typeof(ZoneSphere))]
    public sealed class ZoneSphereEditor : UnityEditor.Editor
    {
        private static readonly Color SphereColor =
            new Color(0.15f, 0.8f, 1f, 1f);

        private static readonly Color HandleColor =
            new Color(1f, 0.75f, 0.1f, 1f);

        private ZoneSphere _sphere;

        private void OnEnable()
        {
            _sphere = (ZoneSphere)target;
        }

        private void OnSceneGUI()
        {
            if (_sphere == null)
                return;

            DrawSphere();
            DrawRadiusHandle();
            DrawCenterHandle();
            DrawLabel();
        }

        private void DrawSphere()
        {
            Transform t = _sphere.transform;

            Handles.color = SphereColor;

            float radius = _sphere.Radius;

            // Основная сфера.
            Handles.DrawWireDisc(
                t.position,
                t.up,
                radius
            );

            Handles.DrawWireDisc(
                t.position,
                t.right,
                radius
            );

            Handles.DrawWireDisc(
                t.position,
                t.forward,
                radius
            );

            // Дополнительная окружность в направлении камеры.
            SceneView sceneView = SceneView.currentDrawingSceneView;

            if (sceneView != null)
            {
                Vector3 cameraDirection =
                    (sceneView.camera.transform.position - t.position).normalized;

                Handles.DrawWireDisc(
                    t.position,
                    cameraDirection,
                    radius
                );
            }
        }

        private void DrawRadiusHandle()
        {
            Transform t = _sphere.transform;

            Vector3 worldDirection = t.right.normalized;

            Vector3 handlePosition =
                t.position + worldDirection * _sphere.Radius;

            float handleSize =
                HandleUtility.GetHandleSize(handlePosition) * 0.12f;

            Handles.color = HandleColor;

            EditorGUI.BeginChangeCheck();

            Vector3 newPosition = Handles.Slider(
                handlePosition,
                worldDirection,
                handleSize,
                Handles.DotHandleCap,
                0f
            );

            if (!EditorGUI.EndChangeCheck())
                return;

            Vector3 localPosition =
                t.InverseTransformPoint(newPosition);

            float newRadius = Mathf.Abs(localPosition.x);

            if (Event.current.shift)
            {
                // Shift — обычное масштабирование всё равно
                // происходит относительно центра.
                newRadius *= 1f;
            }

            if (Event.current.control)
            {
                newRadius = Snap(newRadius);
            }

            newRadius = Mathf.Max(0.01f, newRadius);

            Undo.RecordObject(
                _sphere,
                "Resize Zone Sphere"
            );

            _sphere.Radius = newRadius;

            EditorUtility.SetDirty(_sphere);
        }

        private void DrawCenterHandle()
        {
            Transform t = _sphere.transform;

            float size =
                HandleUtility.GetHandleSize(t.position) * 0.08f;

            Handles.color = HandleColor;

            EditorGUI.BeginChangeCheck();

            Vector3 newPosition = Handles.FreeMoveHandle(
                t.position,
                size,
                Vector3.zero,
                Handles.DotHandleCap
            );

            if (!EditorGUI.EndChangeCheck())
                return;

            Undo.RecordObject(
                t,
                "Move Zone Sphere"
            );

            t.position = newPosition;

            EditorUtility.SetDirty(t);
        }

        private void DrawLabel()
        {
            Transform t = _sphere.transform;

            Vector3 labelPosition =
                t.position + t.up * _sphere.Radius;

            Handles.Label(
                labelPosition,
                $"Radius: {_sphere.Radius:0.##}"
            );
        }

        private static float Snap(float value)
        {
            const float snap = 0.5f;

            return Mathf.Round(value / snap) * snap;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space(8);

            if (GUILayout.Button("Reset Radius"))
            {
                Undo.RecordObject(
                    _sphere,
                    "Reset Zone Sphere Radius"
                );

                _sphere.Radius = 1f;

                EditorUtility.SetDirty(_sphere);
            }

            if (GUILayout.Button("Focus"))
            {
                Selection.activeObject = _sphere;

                SceneView.lastActiveSceneView?.FrameSelected();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif