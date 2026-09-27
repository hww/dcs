using DCS.Core;
using DCS.Spatial;
using UnityEngine;

namespace DCS.Gameplay
{
    public sealed class SpatialBootstrap : MonoBehaviour
    {
        public static SpatialBootstrap Instance { get; private set; }

        [Header("Grid Configuration")]
        [SerializeField] private int _gridWidth = 16;
        [SerializeField] private int _gridHeight = 16;
        [SerializeField] private int _gridDepth = 16;
        [SerializeField] private float _cellSize = 12.5f;

        public NameIndex Names { get; private set; }
        public TagIndex Tags { get; private set; }
        public DynamicSpatial Spatial { get; private set; }
        public DynamicSpatialSystem SpatialSystem { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Names = new NameIndex();
            Tags = new TagIndex();
            Spatial = new DynamicSpatial(_gridWidth, _gridHeight, _gridDepth, _cellSize);

            // Register FastPools (they share the group table).
            ComponentRegistry.RegisterFastPool<PositionComponent>(capacity: 16384);
            ComponentRegistry.RegisterFastPool<NameComponent>(capacity: 4096);
            ComponentRegistry.RegisterFastPool<TagComponent>(capacity: 4096);

            // Build sources.
            var posSource = new FastPoolPositionSource(ComponentRegistry.GetFastPool<PositionComponent>());
            var nameSource = new FastPoolNameSource(ComponentRegistry.GetFastPool<NameComponent>());
            var tagSource = new FastPoolTagSource(ComponentRegistry.GetFastPool<TagComponent>());

            SpatialSystem = new DynamicSpatialSystem(
                Spatial, Names, Tags,
                posSource, nameSource, tagSource,
                radius: 0.5f);

            ActorRegistryFacade.Initialize(Names, Tags, Spatial);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}