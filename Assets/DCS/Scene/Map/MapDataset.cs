using System;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicComponent
{
    /// <summary>
    /// Не содержит MonoBehaviour. Чистые скомпилированные данные одной локации.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMapDataset", menuName = "DCS Engine/Map Dataset")]
    public class MapDataset : ScriptableObject
    {
        [Header("Map Metadata")]
        public string MapName;

        [Header("Gameplay Records (Таблицы сущностей)")]
        public List<ZoneRecord> Zones = new List<ZoneRecord>();
        public List<SpawnerRecord> Spawners = new List<SpawnerRecord>();
        public List<LocatorRecord> Locators = new List<LocatorRecord>();

        [Header("Spatial Compiled Data (Плоский индекс геометрии)")]
        // Весь мир с точки зрения геометрии — просто массив этих записей
        public List<SpatialShapeRecord> SpatialShapes = new List<SpatialShapeRecord>();
    }

    // Сухие структуры данных (Data Records) без логики Unity
    [Serializable]
    public struct ZoneRecord
    {
        public ushort HostId;
        public ushort Generation;
        public Vector3 Position;
        public float ActivationRadius;
        public string FactsJson;
    }

    [Serializable]
    public struct SpawnerRecord
    {
        public ushort HostId;
        public Vector3 Position;
        public string EntityClass;
    }

    [Serializable]
    public struct LocatorRecord
    {
        public ushort HostId;
        public Vector3 Position;
        public string TagsJson;
    }
}
