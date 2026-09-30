using DCS.Data;
using DCS.Debugging.Runtime;
using UnityEditor;
using UnityEngine;

namespace DCS.Debugging.Editor
{
    [CustomEditor(typeof(MapDatasetDebugView))]
    [CanEditMultipleObjects]
    public sealed class MapDatasetDebugViewEditor : UnityEditor.Editor
    {
        private bool _showLayers = true;

        public override void OnInspectorGUI()
        {
            var view = (MapDatasetDebugView)target;

            serializedObject.Update();

            // Рисуем поле Dataset и класс настроек
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_dataset"));

            EditorGUILayout.Space(4f);
            _showLayers = EditorGUILayout.Foldout(_showLayers, "Debug Layer Toggles", true);
            if (_showLayers)
            {
                SerializedProperty settingsProp = serializedObject.FindProperty("_settings");
                if (settingsProp != null)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("ShowSpawners"));
                    EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("ShowLocators"));
                    EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("ShowPatrolPoints"));
                    EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("ShowStrongPoints"));
                    EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("ShowPosts"));
                    EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("ShowTraversalLinks"));
                    EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("ShowNavigationSurfaces"));
                    EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("ShowPolygons"));
                    EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("ShowSpheres"));
                    EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("ShowBoxes"));
                    EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("ShowCylinders"));
                    EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("ShowLabels"));
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(view.Dataset == null))
                {
                    if (GUILayout.Button("Frame All"))
                        FrameAll(view);

                    if (GUILayout.Button("Select Asset"))
                        Selection.activeObject = view.Dataset;
                }
            }

            EditorGUILayout.Space(4f);

            if (view.Dataset == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a MapDataset asset to visualize its contents.",
                    MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            DrawSummary(view.Dataset);

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawSummary(MapDataset d)
        {
            EditorGUILayout.LabelField("Dataset Summary", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Map Name", string.IsNullOrEmpty(d.MapName) ? "(unnamed)" : d.MapName);
            EditorGUILayout.LabelField("Encounters", d.Encounters.Count.ToString());
            EditorGUILayout.LabelField("Regions", d.Regions.Count.ToString());
            EditorGUILayout.LabelField("Triggers", d.Triggers.Count.ToString());
            EditorGUILayout.LabelField("Strong Points", d.StrongPoints.Count.ToString());
            EditorGUILayout.LabelField("Spawners", d.Spawners.Count.ToString());
            EditorGUILayout.LabelField("Locators", d.Locators.Count.ToString());
            EditorGUILayout.LabelField("Posts", d.Posts.Count.ToString());
            EditorGUILayout.LabelField("Patrol Points", d.PatrolPoints.Count.ToString());
            EditorGUILayout.LabelField("Traversal Links", d.TraversalLinks.Count.ToString());
            EditorGUILayout.LabelField("Geometries (S/B/C/P)", $"{d.Spheres.Count} / {d.Boxes.Count} / {d.Cylinders.Count} / {d.Polygons.Count}");

            EditorGUI.indentLevel--;
        }

        private static void FrameAll(MapDatasetDebugView view)
        {
            if (!MapDatasetDebugView.TryGetBounds(view.Dataset, out var bounds))
                return;

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.Frame(bounds, false);
                sceneView.Repaint();
            }
        }
    }
}
