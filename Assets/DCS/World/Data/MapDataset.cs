using System.Collections.Generic;
using UnityEngine;

namespace DynamicComponent
{
    [CreateAssetMenu(fileName = "NewMapDataset", menuName = "DCS Engine/Map Dataset")]
    public class MapDataset : ScriptableObject
    {
        [Header("Map Metadata")]
        public string MapName;

        [Header("Gameplay Records")]
        public List<ZoneRecord> Zones = new List<ZoneRecord>();
        public List<SpawnerRecord> Spawners = new List<SpawnerRecord>();
        public List<LocatorRecord> Locators = new List<LocatorRecord>();

        [Header("Spatial Broadphase")]
        public List<SpatialProxy> SpatialProxies = new List<SpatialProxy>();

        [Header("Spatial Geometry")]
        public List<SphereGeometry> Spheres = new List<SphereGeometry>();
        public List<BoxGeometry> Boxes = new List<BoxGeometry>();
        public List<TriangleGeometry> Triangles = new List<TriangleGeometry>();

        public void Clear()
        {
            Zones.Clear();
            Spawners.Clear();
            Locators.Clear();
            SpatialProxies.Clear();
            Spheres.Clear();
            Boxes.Clear();
            Triangles.Clear();
        }
    }
}
