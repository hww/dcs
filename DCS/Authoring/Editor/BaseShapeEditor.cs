using UnityEditor;
using UnityEngine;

namespace DCS.Authoring.Editor
{
    /// <summary>
    /// Base editor for every DCS authoring shape.
    /// Provides the common Enabled toggle, an authoring hint, and the
    /// Scene-view lifecycle hooks used by derived editors.
    /// </summary>
    public abstract class BaseShapeEditor : UnityEditor.Editor
    {
        protected SerializedProperty EnabledProp;

        protected virtual void OnEnable()
        {
            EnabledProp = serializedObject.FindProperty("_enabled");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ShapeEditorGUIUtility.DrawHeader(TargetDisplayName, "Authoring Shape");
            ShapeEditorGUIUtility.DrawEnabledToggle(EnabledProp);

            EditorGUILayout.Space(4f);
            DrawShapeInspector();
            EditorGUILayout.Space(4f);

            ShapeEditorGUIUtility.InfoBox(
                "Authoring-only shape. Geometry is consumed by the Build / Spatial pipeline; " +
                "no runtime queries are executed by this component.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>Human readable name shown in the inspector header.</summary>
        protected virtual string TargetDisplayName => ObjectNames.NicifyVariableName(target.GetType().Name);

        /// <summary>Draws shape-specific controls. Override in derived editors.</summary>
        protected abstract void DrawShapeInspector();

        /// <summary>
        /// Called from <see cref="OnSceneGUI"/>. Override to add handles.
        /// </summary>
        protected virtual void DrawShapeHandles() { }

        /// <summary>
        /// Called from <see cref="OnSceneGUI"/> to draw non-interactive gizmos
        /// (fills, labels, wireframes) on top of the Unity gizmo pass.
        /// </summary>
        protected virtual void DrawShapeSceneGizmos() { }

        protected virtual void OnSceneGUI()
        {
            var shape = (BaseShape)target;
            if (!shape.Enabled)
                return;

            DrawShapeSceneGizmos();
            DrawShapeHandles();
        }

        /// <summary>
        /// Convenience helper: records a deep undo snapshot on the target.
        /// </summary>
        protected void RecordUndo(string label)
        {
            Undo.RecordObject(target, label);
        }

        /// <summary>
        /// Convenience helper: records an undo on the serialized object and
        /// flags it dirty so the change persists.
        /// </summary>
        protected void RecordSerializedUndo(string label)
        {
            Undo.RecordObject(target, label);
            EditorUtility.SetDirty(target);
        }
    }
}