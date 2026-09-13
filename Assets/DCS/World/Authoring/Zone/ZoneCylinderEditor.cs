#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace DynamicComponent.Editor
{
    [CustomEditor(typeof(ZoneCylinder))]
    public sealed class ZoneCylinderEditor : UnityEditor.Editor
    {
        private static readonly Color CylinderColor =
            new Color(0.15f, 0.8f, 1f, 1f);

        private static readonly Color HandleColor =
            new Color(1f, 0.75f, 0.1f, 1f);

        private ZoneCylinder _cylinder;

        private void OnEnable()
        {
            _cylinder = (ZoneCylinder)target;
        }

        private void OnSceneGUI()
        {
            if (_cylinder == null)
                return;

            DrawCylinder();
            DrawRadiusHandle();
            DrawHeightHandles();
            DrawCenterHandle();
            DrawLabels();
        }

        private void DrawCylinder()
        {
            Transform t = _cylinder.transform;

            float radius = _cylinder.Radius;
            float halfHeight = _cylinder.Height * 0.5f;

            Vector3 top =
                t.TransformPoint(Vector3.up * halfHeight);

            Vector3 bottom =
                t.TransformPoint(Vector3.down * halfHeight);

            Handles.color = CylinderColor;

            // Верх и низ.
            Handles.DrawWireDisc(
                top,
                t.up,
                radius
            );

            Handles.DrawWireDisc(
                bottom,
                t.up,
                radius
            );

            // Боковые линии.
            Vector3[] directions =
            {
                Vector3.right,
                Vector3.forward,
                Vector3.left,
                Vector3.back
            };

            foreach (Vector3 direction in directions)
            {
                Vector3 localOffset =
                    direction * radius;

                Handles.DrawLine(
                    t.TransformPoint(localOffset + Vector3.up * halfHeight),
                    t.TransformPoint(localOffset + Vector3.down * halfHeight)
                );
            }
        }

        private void DrawRadiusHandle()
        {
            Transform t = _cylinder.transform;

            float halfHeight = _cylinder.Height * 0.5f;

            Vector3 localPosition =
                Vector3.right * _cylinder.Radius +
                Vector3.up * halfHeight;

            Vector3 worldPosition =
                t.TransformPoint(localPosition);

            Vector3 direction =
                t.TransformDirection(Vector3.right).normalized;

            float handleSize =
                HandleUtility.GetHandleSize(worldPosition) * 0.12f;

            Handles.color = HandleColor;

            EditorGUI.BeginChangeCheck();

            Vector3 newPosition = Handles.Slider(
                worldPosition,
                direction,
                handleSize,
                Handles.DotHandleCap,
                0f
            );

            if (!EditorGUI.EndChangeCheck())
                return;

            Vector3 local =
                t.InverseTransformPoint(newPosition);

            float radius = Mathf.Sqrt(
                local.x * local.x +
                local.z * local.z
            );

            if (Event.current.control)
                radius = Snap(radius);

            radius = Mathf.Max(0.01f, radius);

            Undo.RecordObject(
                _cylinder,
                "Resize Zone Cylinder Radius"
            );

            _cylinder.Radius = radius;

            EditorUtility.SetDirty(_cylinder);
        }

        private void DrawHeightHandles()
        {
            Transform t = _cylinder.transform;

            float halfHeight = _cylinder.Height * 0.5f;

            Vector3 top =
                t.TransformPoint(
                    Vector3.up * halfHeight
                );

            Vector3 bottom =
                t.TransformPoint(
                    Vector3.down * halfHeight
                );

            DrawHeightHandle(
                top,
                true
            );

            DrawHeightHandle(
                bottom,
                false
            );
        }

        private void DrawHeightHandle(
            Vector3 position,
            bool top
        )
        {
            Transform t = _cylinder.transform;

            Vector3 direction =
                t.up.normalized * (top ? 1f : -1f);

            float handleSize =
                HandleUtility.GetHandleSize(position) * 0.12f;

            Handles.color = HandleColor;

            EditorGUI.BeginChangeCheck();

            Vector3 newPosition = Handles.Slider(
                position,
                direction,
                handleSize,
                Handles.DotHandleCap,
                0f
            );

            if (!EditorGUI.EndChangeCheck())
                return;

            Vector3 local =
                t.InverseTransformPoint(newPosition);

            float newHalfHeight =
                Mathf.Abs(local.y);

            float newHeight =
                newHalfHeight * 2f;

            if (Event.current.control)
                newHeight = Snap(newHeight);

            newHeight = Mathf.Max(0.01f, newHeight);

            Undo.RecordObject(
                _cylinder,
                "Resize Zone Cylinder Height"
            );

            if (Event.current.shift)
            {
                // Симметричное изменение относительно центра.
                _cylinder.Height = newHeight;
            }
            else
            {
                ResizeFromOneSide(
                    top,
                    newPosition
                );
            }

            EditorUtility.SetDirty(_cylinder);
        }

        private void ResizeFromOneSide(
            bool top,
            Vector3 worldPosition
        )
        {
            Transform t = _cylinder.transform;

            Vector3 localPosition =
                t.InverseTransformPoint(worldPosition);

            float currentHalfHeight =
                _cylinder.Height * 0.5f;

            float opposite =
                top
                    ? -currentHalfHeight
                    : currentHalfHeight;

            float newHeight =
                Mathf.Abs(localPosition.y - opposite);

            newHeight = Mathf.Max(0.01f, newHeight);

            float newCenterY =
                (localPosition.y + opposite) * 0.5f;

            Vector3 localCenter =
                t.InverseTransformPoint(t.position);

            localCenter.y = newCenterY;

            Vector3 worldCenter =
                t.TransformPoint(localCenter);

            t.position = worldCenter;

            _cylinder.Height = newHeight;

            EditorUtility.SetDirty(t);
        }

        private void DrawCenterHandle()
        {
            Transform t = _cylinder.transform;

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
                "Move Zone Cylinder"
            );

            t.position = newPosition;

            EditorUtility.SetDirty(t);
        }

        private void DrawLabels()
        {
            Transform t = _cylinder.transform;

            float halfHeight =
                _cylinder.Height * 0.5f;

            Vector3 radiusLabel =
                t.TransformPoint(
                    Vector3.right * _cylinder.Radius +
                    Vector3.up * halfHeight
                );

            Vector3 heightLabel =
                t.TransformPoint(
                    Vector3.up * halfHeight
                );

            Handles.Label(
                radiusLabel,
                $"Radius: {_cylinder.Radius:0.##}"
            );

            Handles.Label(
                heightLabel,
                $"Height: {_cylinder.Height:0.##}"
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

            if (GUILayout.Button("Reset"))
            {
                Undo.RecordObject(
                    _cylinder,
                    "Reset Zone Cylinder"
                );

                _cylinder.Radius = 1f;
                _cylinder.Height = 2f;

                EditorUtility.SetDirty(_cylinder);
            }

            if (GUILayout.Button("Focus"))
            {
                Selection.activeObject = _cylinder;

                SceneView.lastActiveSceneView?.FrameSelected();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif