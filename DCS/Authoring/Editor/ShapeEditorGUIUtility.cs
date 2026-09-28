using UnityEditor;
using UnityEngine;

namespace DCS.Authoring.Editor
{
    /// <summary>
    /// Shared IMGUI/Handles helpers for the DCS spatial shape editors.
    /// </summary>
    internal static class ShapeEditorGUIUtility
    {
        public static readonly Color FillColor = new Color(0.20f, 0.65f, 1.00f, 0.10f);
        public static readonly Color OutlineColor = new Color(0.20f, 0.65f, 1.00f, 1.00f);
        public static readonly Color HandleColor = new Color(1.00f, 0.80f, 0.20f, 1.00f);
        public static readonly Color SelectedColor = new Color(1.00f, 0.45f, 0.10f, 1.00f);

        /// <summary>
        /// Draws the section header used at the top of each shape editor.
        /// </summary>
        public static void DrawHeader(string title, string subtitle = null)
        {
            EditorGUILayout.Space(2f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(title, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (!string.IsNullOrEmpty(subtitle))
                {
                    var style = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleRight
                    };
                    GUILayout.Label(subtitle, style);
                }
            }
            EditorGUILayout.Space(2f);
        }

        /// <summary>
        /// Draws the "Enabled" toggle with a coloured status indicator.
        /// </summary>
        public static void DrawEnabledToggle(SerializedProperty enabledProp)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                bool value = EditorGUILayout.ToggleLeft("Enabled", enabledProp.boolValue, EditorStyles.boldLabel);
                if (EditorGUI.EndChangeCheck())
                    enabledProp.boolValue = value;

                GUILayout.FlexibleSpace();

                var prev = GUI.color;
                GUI.color = enabledProp.boolValue
                    ? new Color(0.35f, 0.85f, 0.35f)
                    : new Color(0.85f, 0.35f, 0.35f);
                GUILayout.Label(enabledProp.boolValue ? "\u25CF Active" : "\u25CF Disabled",
                    EditorStyles.miniLabel);
                GUI.color = prev;
            }
        }

        /// <summary>
        /// Reusable numeric field with clamping and a min/max hint.
        /// </summary>
        public static float FloatField(string label, float value, float min, float max = float.MaxValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            value = EditorGUILayout.FloatField(value);
            value = Mathf.Clamp(value, min, max);
            EditorGUILayout.EndHorizontal();
            return value;
        }

        /// <summary>
        /// Draws a small help box that respects the current skin.
        /// </summary>
        public static void InfoBox(string message, MessageType type = MessageType.Info)
        {
            EditorGUILayout.HelpBox(message, type);
        }
    }
}