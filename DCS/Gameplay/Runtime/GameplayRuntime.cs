using UnityEngine;
using DCS.Spatial;

namespace DCS.Gameplay
{
    /// <summary>
    /// Coordinates runtime gameplay systems for the currently loaded MapDataset.
    /// Does not contain gameplay rules itself.
    /// </summary>
    public sealed class GameplayRuntime : MonoBehaviour
    {
        public static GameplayRuntime Instance { get; private set; }

        public MapDataset CurrentDataset { get; private set; }

        public EncounterRuntimeRegistry Encounters { get; private set; }
        public TriggerSystem Triggers { get; private set; }

        public bool HasData => CurrentDataset != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Encounters = new EncounterRuntimeRegistry();
            Triggers = new TriggerSystem();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Load(MapDataset dataset)
        {
            Clear();

            if (dataset == null)
            {
                Debug.LogError("[GameplayRuntime] Dataset is null.");
                return;
            }

            CurrentDataset = dataset;

            if (SpatialRuntime.Instance != null)
                SpatialRuntime.Instance.Load(dataset);

            Encounters.Load(dataset.Encounters);
            Triggers.Load(dataset.Triggers);
        }

        public void Clear()
        {
            Triggers?.Clear();
            Encounters?.Clear();

            if (SpatialRuntime.Instance != null)
                SpatialRuntime.Instance.Clear();

            CurrentDataset = null;
        }
    }
}