using DCS.Actors;
using DCS.Core;
using DCS.Lua;
using DCS.Spatial;
using System.Collections.Generic;
using UnityEngine;

namespace DCS.Game.SoldierCS
{
    /// <summary>
    /// Root игры SoldierCS.
    /// Хардкод порядка обновления. Никаких ISystem.
    /// </summary>
    public sealed class SoldierRoot : MonoBehaviour
    {
        // --- Singleton ---
        private static SoldierRoot _instance;
        public static SoldierRoot Instance => _instance;

        public static SoldierRoot Ensure()
        {
            if (_instance != null) return _instance;

            var go = new GameObject("[SoldierRoot]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SoldierRoot>();
            _instance.Initialize();
            return _instance;
        }

        [Header("Prefab")]
        public GameObject SoldierPrefab;

        [Header("Spawn")]
        public int SoldierCount = 1;
        public Vector3 SpawnOrigin = Vector3.zero;
        public float SpawnSpacing = 2f;

        [Header("World")]
        public int Capacity = 100;

        [Header("Camera")]
        public ThirdPersonCamera Camera;

        // --- Contexts ---
        public Domain Domain { get; private set; }
        public int SpatialDomainId { get; private set; } = Domain.NoSpatial;

        // --- Time ---
        public DCSTime TimeFrame { get; private set; }

        // --- Internal ---
        private bool _initialized;
        private int _soldierCounter;
        private Host _playerHost;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            if (!_initialized) Initialize();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            Deinitialize();
        }
        private SpatialUpdateSystem _spatialUpdate;
        private void Initialize()
        {
            _initialized = true;

            // 1. Time
            TimeFrame = new DCSTime();

            // 2. Domain
            Domain = DomainRegistry.Create("SoldierGame");

            // 3.1 Все ComponentPool'ы (через атрибуты [ComponentPool]).
            ComponentRegistry.InitializeAllPools();

            SpatialDomainId = SpatialDomainRegistry.Create(
                gridWidth: 16, gridHeight: 16, gridDepth: 16,
                cellSize: 12.5f);

            Domain.AttachSpatial(SpatialDomainId);
            var spatialDomain = SpatialDomainRegistry.Get(SpatialDomainId);
            _spatialUpdate = new SpatialUpdateSystem(
                spatialDomain.DynamicSpatial,
                ComponentRegistry.GetPool<PositionComponent>(),
                radius: 0.5f);

            // 5. Spawn
            for (int i = 0; i < SoldierCount; i++)
                SpawnSoldier(i == 0);
        }

        private void Deinitialize()
        {
            if (Domain != null && Domain.HasSpatial)
            {
                SpatialDomainRegistry.Remove(Domain.SpatialDomainId);
                Domain.DetachSpatial();
            }
        }

        private void SpawnSoldier(bool isPlayer)
        {
            int index = _soldierCounter++;

            Vector3 pos = SpawnOrigin + new Vector3(index * SpawnSpacing, 0f, 0f);
            GameObject go = Instantiate(SoldierPrefab, pos, Quaternion.identity);
            go.name = isPlayer ? "Player" : $"Soldier_{index}";

            Actor actor = go.GetComponentInChildren<Actor>();

            Host host = HostManager.CreateHost();
            HostManager.LinkHostReference(host, go.GetComponent<IHostReference>());

            // PositionComponent — через ComponentPool.
            Handle hPos = DCSystem.Allocate<PositionComponent>(host, Domain.HostChain);
            ref var posComp = ref DCSystem.ResolveHandle<PositionComponent>(hPos);
            posComp.Position = pos;
            posComp.Rotation = Quaternion.identity;

            // NameComponent.
            Handle hName = DCSystem.Allocate<NameComponent>(host, Domain.HostChain);
            ref var nameComp = ref DCSystem.ResolveHandle<NameComponent>(hName);
            nameComp.Name = isPlayer ? "Player" : $"Soldier_{index}";

            // ArchetypeComponent.
            Handle hType = DCSystem.Allocate<ArchetypeComponent>(host, Domain.HostChain);
            ref var atpComp = ref DCSystem.ResolveHandle<ArchetypeComponent>(hType);
            atpComp.Archetype = isPlayer ? "Player" : "Enemy";

            // Остальные — через обычный ComponentPool.
            DCSystem.ResolveHandle<VelocityComponent>(
                DCSystem.Allocate<VelocityComponent>(host, Domain.HostChain)).Value = Vector3.zero;
            DCSystem.ResolveHandle<InputComponent>(
                DCSystem.Allocate<InputComponent>(host, Domain.HostChain));
            DCSystem.ResolveHandle<CombatStateComponent>(
                DCSystem.Allocate<CombatStateComponent>(host, Domain.HostChain)).Value = ECombatState.Combat;
            DCSystem.ResolveHandle<LocomotionComponent>(
                DCSystem.Allocate<LocomotionComponent>(host, Domain.HostChain)).Value = ELocomotion.Idle;

            // ViewComponent — Unity-ссылки.
            Handle hView = DCSystem.Allocate<ViewComponent>(host, Domain.HostChain);
            ref ViewComponent view = ref DCSystem.ResolveHandle<ViewComponent>(hView);
            view.Actor = actor;

            if (isPlayer)
            {
                _playerHost = host;
                if (Camera != null)
                    Camera.Target = actor.transform;
            }
        }

        private void Update()
        {
            // 1. Time
            TimeFrame.Update();

            // 2. Player input
            if (Camera != null)
                PlayerInputSystem.Update(_playerHost, Domain.HostChain, Camera);

            // 3. Movement
            MovementSystem.Update(Domain.HostChain, Camera != null ? Camera.transform : null, TimeFrame.DeltaTime);

            // 4. Animation
            SoldierAnimationSystem.Update(Domain.HostChain);

            // 5. Spatial — после всех, кто двигает.
            _spatialUpdate.Update();

            // 6. Event delivery — C#-получатели + Lua (если есть).
            EventSystem.DeliverAll(Domain, Dispatch);

            // 7. Transform sync — в конце, чтобы визуал соответствовал.
            TransformSyncSystem.Update(Domain.HostChain);
        }

        private void Dispatch(in SubscriptionNode sub, Host sender, int eventTypeId, Handle messageHandle)
        {
            // C#-получатели. Если в игре нет Lua — этого достаточно.
            var pool = ComponentRegistry.Pools[sub.ProcessTypeId];
            pool?.SystemDeliver(sub.ProcessHandle.Id, eventTypeId, messageHandle);
        }
    }

}