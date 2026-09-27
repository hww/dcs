using DCS.Authoring;
using DCS.Core;
using DCS.Data;
using DCS.Spatial;
using UnityEngine;

namespace DCS.World
{
    /// <summary>
    /// Мир игры: текущий датасет, encounters, triggers.
    /// Не MonoBehaviour. Живёт как поле в Root.
    /// </summary>
    public sealed class WorldRuntime
    {
        public MapDataset CurrentDataset { get; private set; }
        public EncounterRuntimeRegistry Encounters { get; private set; }
        public TriggerSystem Triggers { get; private set; }
        public bool HasData => CurrentDataset != null;

        public WorldRuntime()
        {
            Encounters = new EncounterRuntimeRegistry();
            Triggers = new TriggerSystem();
        }

        /// <summary>
        /// Загружает датасет в world. SpatialDomain передаётся, чтобы
        /// загрузить в него static spatial.
        /// </summary>
        public void Load(MapDataset dataset, SpatialDomain spatial)
        {
            Clear(spatial);
            if (dataset == null)
            {
                Debug.LogError("[WorldRuntime] Dataset is null.");
                return;
            }

            CurrentDataset = dataset;

            if (spatial != null)
            {
                spatial.StaticSpatial ??= new SpatialRuntime(0);
                spatial.StaticSpatial.Load(dataset);
            }

            Encounters.Load(dataset.Encounters);
            Triggers.Load(dataset.Triggers);
        }

        public void Clear(SpatialDomain spatial)
        {
            Triggers?.Clear();
            Encounters?.Clear();
            spatial?.StaticSpatial?.Clear();
            CurrentDataset = null;
        }
    }
}