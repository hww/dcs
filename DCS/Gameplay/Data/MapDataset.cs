using System.Collections.Generic;
using DCS.Spatial;
using UnityEngine;

namespace DCS.Gameplay
{
    [CreateAssetMenu(fileName = "NewMapDataset", menuName = "DCS/World/Map Dataset")]
    public sealed class MapDataset : ScriptableObject
    {
        public string MapName;
        public List<EncounterRecord> Encounters = new List<EncounterRecord>();
        public List<StrongPointRecord> StrongPoints = new List<StrongPointRecord>();
        public List<RegionRecord> Regions = new List<RegionRecord>();
        public List<TriggerRecord> Triggers = new List<TriggerRecord>();
        public List<NavigationSurfaceRecord> NavigationSurfaces = new List<NavigationSurfaceRecord>();
        public List<TraversalLinkRecord> TraversalLinks = new List<TraversalLinkRecord>();
        public List<PatrolPathRecord> PatrolPaths = new List<PatrolPathRecord>();
        public List<Vector3> PatrolPoints = new List<Vector3>();
        public List<PostRecord> Posts = new List<PostRecord>();
        public List<SpawnerRecord> Spawners = new List<SpawnerRecord>();
        public List<LocatorRecord> Locators = new List<LocatorRecord>();

        public List<SpatialProxy> SpatialProxies = new List<SpatialProxy>();
        public List<SphereGeometry> Spheres = new List<SphereGeometry>();
        public List<BoxGeometry> Boxes = new List<BoxGeometry>();
        public List<CylinderGeometry> Cylinders = new List<CylinderGeometry>();
        public List<PolygonGeometry> Polygons = new List<PolygonGeometry>();
        public List<Vector3> PolygonPoints = new List<Vector3>();

        public void Clear()
        {
            Encounters.Clear(); StrongPoints.Clear(); Regions.Clear(); Triggers.Clear();
            NavigationSurfaces.Clear(); TraversalLinks.Clear(); PatrolPaths.Clear(); PatrolPoints.Clear(); Posts.Clear();
            Spawners.Clear(); Locators.Clear(); SpatialProxies.Clear(); Spheres.Clear(); Boxes.Clear(); Cylinders.Clear(); Polygons.Clear(); PolygonPoints.Clear();
        }
    }
}
