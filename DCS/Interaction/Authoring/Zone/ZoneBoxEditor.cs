#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using DCS.Core;

namespace DCS.Core.Editor
{
    [CustomEditor(typeof(ZoneBox))]
    public sealed class ZoneBoxEditor : UnityEditor.Editor
    {
        private static readonly Color BoxColor = new Color(0.1f, 0.8f, 1f, 1f);
        private static readonly Color HandleColor = new Color(1f, 0.8f, 0.1f, 1f);

        private ZoneBox _box;

        private void OnEnable()
        {
            _box = (ZoneBox)target;
        }

        private void OnSceneGUI()
        {
            if (_box == null)
                return;

            DrawBox();
            DrawHandles();
            DrawSizeLabel();
        }

        private void DrawBox()
        {
            Transform t = _box.transform;

            Handles.color = BoxColor;

            Vector3 half = _box.Size * 0.5f;

            Vector3[] corners =
            {
                new Vector3(-half.x, -half.y, -half.z),
                new Vector3( half.x, -half.y, -half.z),
                new Vector3( half.x, -half.y,  half.z),
                new Vector3(-half.x, -half.y,  half.z),

                new Vector3(-half.x,  half.y, -half.z),
                new Vector3( half.x,  half.y, -half.z),
                new Vector3( half.x,  half.y,  half.z),
                new Vector3(-half.x,  half.y,  half.z)
            };

            for (int i = 0; i < 4; i++)
            {
                Handles.DrawLine(
                    t.TransformPoint(corners[i]),
                    t.TransformPoint(corners[(i + 1) % 4])
                );

                Handles.DrawLine(
                    t.TransformPoint(corners[i + 4]),
                    t.TransformPoint(corners[((i + 1) % 4) + 4])
                );

                Handles.DrawLine(
                    t.TransformPoint(corners[i]),
                    t.TransformPoint(corners[i + 4])
                );
            }
        }

        private void DrawHandles()
        {
            Transform t = _box.transform;

            Vector3 half = _box.Size * 0.5f;

            Handles.color = HandleColor;

            DrawAxisHandle(
                t.TransformPoint(new Vector3(half.x, 0f, 0f)),
                Vector3.right,
                0
            );

            DrawAxisHandle(
                t.TransformPoint(new Vector3(-half.x, 0f, 0f)),
                Vector3.left,
                0
            );

            DrawAxisHandle(
                t.TransformPoint(new Vector3(0f, half.y, 0f)),
                Vector3.up,
                1
            );

            DrawAxisHandle(
                t.TransformPoint(new Vector3(0f, -half.y, 0f)),
                Vector3.down,
                1
            );

            DrawAxisHandle(
                t.TransformPoint(new Vector3(0f, 0f, half.z)),
                Vector3.forward,
                2
            );

            DrawAxisHandle(
                t.TransformPoint(new Vector3(0f, 0f, -half.z)),
                Vector3.back,
                2
            );
        }

        private void DrawAxisHandle(
            Vector3 worldPosition,
            Vector3 localDirection,
            int axis)
        {
            float size = HandleUtility.GetHandleSize(worldPosition) * 0.12f;

            EditorGUI.BeginChangeCheck();

            Vector3 newPosition = Handles.Slider(
                worldPosition,
                _box.transform.TransformDirection(localDirection),
                size,
                Handles.DotHandleCap,
                0f
            );

            if (!EditorGUI.EndChangeCheck())
                return;

            Vector3 localPosition =
                _box.transform.InverseTransformPoint(newPosition);

            Vector3 currentSize = _box.Size;

            float halfSize = Mathf.Abs(localPosition[axis]) * 2f;

            currentSize[axis] = Mathf.Max(0.01f, halfSize);

            Undo.RecordObject(
                _box,
                "Resize Zone Box"
            );

            _box.Size = currentSize;

            EditorUtility.SetDirty(_box);
        }

        private void DrawSizeLabel()
        {
            Transform t = _box.transform;

            Vector3 labelPosition =
                t.TransformPoint(
                    new Vector3(
                        0f,
                        _box.Size.y * 0.5f,
                        0f
                    )
                );

            Handles.Label(
                labelPosition,
                $"Size {_box.Size.x:0.##} × {_box.Size.y:0.##} × {_box.Size.z:0.##}"
            );
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space(8);

            if (GUILayout.Button("Reset Size"))
            {
                Undo.RecordObject(_box, "Reset Zone Box Size");

                _box.Size = Vector3.one;

                EditorUtility.SetDirty(_box);
            }

            if (GUILayout.Button("Focus"))
            {
                Selection.activeObject = _box;

                SceneView.lastActiveSceneView?.FrameSelected();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif