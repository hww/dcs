using UnityEditor;
using UnityEngine;

namespace DCS.Authoring.Editor
{
    [CustomEditor(typeof(ShapeBox))]
    [CanEditMultipleObjects]
    public sealed class ShapeBoxEditor : BaseShapeEditor
    {
        private SerializedProperty _sizeProp;

        private const float MinSize = 0.01f;

        protected override void OnEnable()
        {
            base.OnEnable();
            _sizeProp = serializedObject.FindProperty("_size");
        }

        protected override string TargetDisplayName => "Shape Box";

        protected override void DrawShapeInspector()
        {
            EditorGUILayout.LabelField("Dimensions", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            Vector3 newSize = EditorGUILayout.Vector3Field("Size", _sizeProp.vector3Value);
            if (EditorGUI.EndChangeCheck())
            {
                newSize.x = Mathf.Max(MinSize, newSize.x);
                newSize.y = Mathf.Max(MinSize, newSize.y);
                newSize.z = Mathf.Max(MinSize, newSize.z);
                _sizeProp.vector3Value = newSize;
            }

            EditorGUI.indentLevel--;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset to 1\u00B3"))
                {
                    _sizeProp.vector3Value = Vector3.one;
                    serializedObject.ApplyModifiedProperties();
                }

                if (GUILayout.Button("Fit to Mesh Bounds"))
                    FitToMeshBounds();
            }
        }

        private void FitToMeshBounds()
        {
            var filter = ((ShapeBox)target).GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                Debug.LogWarning("[ShapeBox] No MeshFilter with a shared mesh found on this GameObject.");
                return;
            }

            RecordSerializedUndo("Fit ShapeBox to Mesh Bounds");
            _sizeProp.vector3Value = filter.sharedMesh.bounds.size;
            serializedObject.ApplyModifiedProperties();
        }

        protected override void DrawShapeSceneGizmos()
        {
            var box = (ShapeBox)target;
            Vector3 size = box.Size;

            Matrix4x4 prev = Handles.matrix;
            Handles.matrix = Matrix4x4.TRS(
                box.transform.position,
                box.transform.rotation,
                box.transform.lossyScale);

            var prevColor = Handles.color;
            Handles.color = ShapeEditorGUIUtility.FillColor;
            Handles.CubeHandleCap(0, Vector3.zero, Quaternion.identity, 1f, EventType.Repaint);

            // Draw an explicit wire cube so the outline stays visible.
            Handles.color = ShapeEditorGUIUtility.OutlineColor;
            Handles.DrawWireCube(Vector3.zero, size);

            Handles.color = prevColor;
            Handles.matrix = prev;
        }

        protected override void DrawShapeHandles()
        {
            var box = (ShapeBox)target;
            Vector3 size = box.Size;
            Vector3 half = size * 0.5f;

            Matrix4x4 trs = Matrix4x4.TRS(
                box.transform.position,
                box.transform.rotation,
                box.transform.lossyScale);

            Matrix4x4 prevMatrix = Handles.matrix;
            Handles.matrix = trs;

            EditorGUI.BeginChangeCheck();
            Vector3 newSize = size;

            // --- Face handles (six pulls) -----------------------------------
            Vector3[] faceNormals =
            {
                Vector3.right, Vector3.left,
                Vector3.up,    Vector3.down,
                Vector3.forward, Vector3.back
            };

            for (int i = 0; i < faceNormals.Length; i++)
            {
                Vector3 n = faceNormals[i];
                Vector3 faceCenter = Vector3.Scale(n, half);
                float handleSize = HandleUtility.GetHandleSize(faceCenter) * 0.12f;

                Handles.color = ShapeEditorGUIUtility.HandleColor;
                Vector3 moved = Handles.Slider(faceCenter, n, handleSize, Handles.CubeHandleCap, 0.01f);

                if (moved != faceCenter)
                {
                    float delta = Vector3.Dot(moved - faceCenter, n);
                    switch (i)
                    {
                        case 0: newSize.x = Mathf.Max(MinSize, size.x + delta * 2f); break;
                        case 1: newSize.x = Mathf.Max(MinSize, size.x + delta * 2f); break;
                        case 2: newSize.y = Mathf.Max(MinSize, size.y + delta * 2f); break;
                        case 3: newSize.y = Mathf.Max(MinSize, size.y + delta * 2f); break;
                        case 4: newSize.z = Mathf.Max(MinSize, size.z + delta * 2f); break;
                        case 5: newSize.z = Mathf.Max(MinSize, size.z + delta * 2f); break;
                    }
                }
            }

            // --- Centre uniform-scale handle --------------------------------
            Handles.color = ShapeEditorGUIUtility.SelectedColor;
            float centerSize = HandleUtility.GetHandleSize(Vector3.zero) * 0.18f;
            var fmh_143_17_639262226840223828 = Quaternion.identity; Vector3 uniform = Handles.FreeMoveHandle(
                Vector3.zero,
                centerSize,
                Vector3.one * 0.05f,
                Handles.RectangleHandleCap);

            if (uniform != Vector3.zero)
            {
                // Use the dominant axis of the drag to scale uniformly.
                float drag = Mathf.Max(
                    Mathf.Abs(uniform.x),
                    Mathf.Abs(uniform.y),
                    Mathf.Abs(uniform.z));
                drag = Mathf.Max(drag, 0.001f);
                newSize = size + Vector3.one * drag * 0.5f;
                newSize.x = Mathf.Max(MinSize, newSize.x);
                newSize.y = Mathf.Max(MinSize, newSize.y);
                newSize.z = Mathf.Max(MinSize, newSize.z);
            }

            if (EditorGUI.EndChangeCheck())
            {
                RecordSerializedUndo("Resize ShapeBox");
                _sizeProp.vector3Value = newSize;
                serializedObject.ApplyModifiedProperties();
            }

            Handles.matrix = prevMatrix;
        }
    }
}