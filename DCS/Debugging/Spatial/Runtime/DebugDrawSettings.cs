using UnityEngine;

namespace DCS.Debugging.Runtime
{
    /// <summary>
    /// Serialized visibility, colour and sizing settings for the MapDataset
    /// debug visualizer. Lives on the scene component, never on the asset.
    /// </summary>
    [System.Serializable]
    public sealed class DebugDrawSettings
    {
        // ---- Visibility toggles -----------------------------------------
        [Header("Visibility")]
        public bool ShowEncounters = true;
        public bool ShowRegions = true;
        public bool ShowTriggers = true;
        public bool ShowStrongPoints = true;
        public bool ShowSpawners = true;
        public bool ShowLocators = true;
        public bool ShowPosts = true;
        public bool ShowPatrolPoints = true;
        public bool ShowTraversalLinks = true;
        public bool ShowNavigationSurfaces = true;
        public bool ShowSpheres = true;
        public bool ShowBoxes = true;
        public bool ShowCylinders = true;
        public bool ShowPolygons = true;

        // ---- Sizing -----------------------------------------------------
        [Header("Sizing")]
        [Min(0.01f)] public float MarkerSize = 0.5f;
        [Min(0.01f)] public float LineWidth = 2f;
        [Min(1)] public int CircleSegments = 24;

        // ---- Labels -----------------------------------------------------
        [Header("Labels")]
        public bool ShowLabels = true;
        public bool ShowHud = true;
        public bool ShowIdsInLabels = true;

        // ---- Highlighting ----------------------------------------------
        [Header("Highlighting")]
        public bool DimDisabledEntries = true;
        [Range(0f, 1f)] public float DisabledAlpha = 0.25f;

        // ---- Colours ----------------------------------------------------
        [Header("Colours")]
        public Color EncounterColor = new Color(0.80f, 0.80f, 0.80f, 0.90f);
        public Color RegionColor = new Color(0.30f, 0.90f, 0.60f, 0.90f);
        public Color TriggerColor = new Color(0.95f, 0.75f, 0.20f, 0.90f);
        public Color StrongPointColor = new Color(1.00f, 0.45f, 0.20f, 0.90f);
        public Color SpawnerColor = new Color(0.25f, 0.90f, 0.25f, 0.90f);
        public Color LocatorColor = new Color(0.95f, 0.95f, 0.30f, 0.90f);
        public Color PostColor = new Color(0.85f, 0.25f, 0.85f, 0.90f);
        public Color PatrolColor = new Color(0.20f, 0.60f, 1.00f, 0.90f);
        public Color TraversalColor = new Color(0.95f, 0.55f, 0.10f, 0.90f);
        public Color NavigationColor = new Color(0.30f, 0.80f, 0.90f, 0.90f);
        public Color SphereColor = new Color(0.40f, 0.70f, 1.00f, 0.60f);
        public Color BoxColor = new Color(0.40f, 1.00f, 0.70f, 0.60f);
        public Color CylinderColor = new Color(1.00f, 0.70f, 0.40f, 0.60f);
        public Color PolygonColor = new Color(0.70f, 0.70f, 1.00f, 0.80f);
        public Color HudBackgroundColor = new Color(0.10f, 0.10f, 0.10f, 0.85f);
        public Color HudTextColor = new Color(0.95f, 0.95f, 0.95f, 1.00f);

        /// <summary>
        /// Returns the input colour with its alpha scaled when the entry is
        /// disabled and the "DimDisabledEntries" option is on.
        /// </summary>
        public Color Resolve(Color baseColor, bool enabled)
        {
            if (!DimDisabledEntries || enabled)
                return baseColor;

            var c = baseColor;
            c.a *= DisabledAlpha;
            return c;
        }
    }
}