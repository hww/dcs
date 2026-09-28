using DCS.Actors;
using DCS.Core;
using DCS.Lua;
using DCS.Spatial;
using UnityEngine;

namespace DCS.Game.SoldierLua
{
    /// <summary>
    /// Root игры SoldierLua.
    /// То же, что SoldierRoot, но с LuaManager как подсистемой.
    /// </summary>
    public sealed class LuaSoldierRoot : MonoBehaviour
    {
        // --- Singleton ---
        private static LuaSoldierRoot _instance;
        public static LuaSoldierRoot Instance => _instance;

        public static LuaSoldierRoot Ensure()
        {
            if (_instance != null) return _instance;

            var go = new GameObject("[LuaSoldierRoot]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<LuaSoldierRoot>();
            _instance.Initialize();
            return _instance;
        }

        [Header("Prefab")]
        public GameObject SoldierPrefab;

        [Header("Spawn")]
        public int SoldierCount = 1;
        public Vector3 SpawnOrigin = Vector3.zero;
        public float SpawnSpacing = 2f;

        [Header("Lua")]
        public bool EnableRepl = true;
        public int ReplPort = 49155;

        // --- Contexts ---
        public Domain Domain { get; private set; }
        public int SpatialDomainId { get; private set; } = Domain.NoSpatial;

        // --- Time ---
        public DCSTime TimeFrame { get; private set; }

        // --- Subsystems ---
        private LuaManager _lua;
        private SpatialUpdateSystem _spatialUpdate;

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

        private void Initialize()
        {
            _initialized = true;

            // 1. Time
            TimeFrame = new DCSTime();

            // 2. Domain
            Domain = DomainRegistry.Create("SoldierLuaGame");

            // 3. ComponentPool'ы
            ComponentRegistry.InitializeAllPools();

            // 4. SpatialDomain
            SpatialDomainId = SpatialDomainRegistry.Create(
                gridWidth: 16, gridHeight: 16, gridDepth: 16,
                cellSize: 12.5f);
            Domain.AttachSpatial(SpatialDomainId);

            var spatialDomain = SpatialDomainRegistry.Get(SpatialDomainId);
            _spatialUpdate = new SpatialUpdateSystem(
                spatialDomain.DynamicSpatial,
                ComponentRegistry.GetPool<PositionComponent>(),
                radius: 0.5f);

            // 5. Lua
            _lua = new LuaManager();
            _lua.Initialize(new LuaManagerConfig
            {
                LuaRootPath = System.IO.Path.Combine(
                    Application.streamingAssetsPath, "Lua").Replace("\\", "/"),
                EnableRepl = EnableRepl,
                ReplPort = ReplPort
            });

            // 6. Spawn
            for (int i = 0; i < SoldierCount; i++)
                SpawnSoldier(i == 0);
        }

        private void Deinitialize()
        {
            _lua?.Deinitialize();
            _lua = null;

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

            // PositionComponent — общий (DCS.Actors).
            Handle hPos = DCSystem.Allocate<PositionComponent>(host, Domain.HostChain);
            ref var posComp = ref DCSystem.ResolveHandle<PositionComponent>(hPos);
            posComp.Position = pos;
            posComp.Rotation = Quaternion.identity;

            // NameComponent — общий.
            Handle hName = DCSystem.Allocate<NameComponent>(host, Domain.HostChain);
            ref var nameComp = ref DCSystem.ResolveHandle<NameComponent>(hName);
            nameComp.Name = isPlayer ? "Player" : $"Soldier_{index}";

            // TagComponent — общий.
            Handle hTag = DCSystem.Allocate<TagComponent>(host, Domain.HostChain);
            ref var tagComp = ref DCSystem.ResolveHandle<TagComponent>(hTag);
            tagComp.Mask = isPlayer ? (1u << 0) : (1u << 1);

            // ViewComponent — из SoldierLua.
            Handle hView = DCSystem.Allocate<ViewComponent>(host, Domain.HostChain);
            ref ViewComponent view = ref DCSystem.ResolveHandle<ViewComponent>(hView);
            view.Actor = actor;

            // VelocityComponent — из SoldierLua.
            Handle hVel = DCSystem.Allocate<VelocityComponent>(host, Domain.HostChain);
            ref VelocityComponent vel = ref DCSystem.ResolveHandle<VelocityComponent>(hVel);
            vel.Value = Vector3.zero;

            // Анимация — зависит от того, на земле или нет.
            // Для примера — GroundedAnimationComponent.
            Handle hAnim = DCSystem.Allocate<GroundedAnimationComponent>(host, Domain.HostChain);
            ref GroundedAnimationComponent anim = ref DCSystem.ResolveHandle<GroundedAnimationComponent>(hAnim);
            anim.Locomotion = 0;  // idle
            anim.Combat = 0;      // combat

            if (isPlayer)
                _playerHost = host;
        }

        private void Update()
        {
            // 1. Time
            TimeFrame.Update();

            // 2. Lua — корутины, каждый кадр.
            _lua?.Update();

            // 3. Ранние системы.
            // PlayerInputSystem.Update(_playerHost, Domain.HostChain, Camera);
            // MovementSystem.Update(Domain.HostChain, null, TimeFrame.DeltaTime);

            // 4. Spatial.
            _spatialUpdate.Update();

            // 5. Доставка событий — Lua + C#.
            EventSystem.DeliverAll(Domain, Dispatch);

            // 6. Синхронизация с Transform.
            // TransformSyncSystem.Update(Domain.HostChain);
        }

        private void Dispatch(in SubscriptionNode sub, Host sender, int eventTypeId, Handle messageHandle)
        {
            // Lua-путь: подписчик — SubscriptionNode.
            if (sub.ProcessTypeId == ComponentType<SubscriptionNode>.Id)
            {
                _lua?.CallEventRouter(sender.Id, eventTypeId, messageHandle.Pack());
                return;
            }

            // C#-путь: подписчик — компонент, реализующий IMessageReceiver.
            var pool = ComponentRegistry.Pools[sub.ProcessTypeId];
            pool?.SystemDeliver(sub.ProcessHandle.Id, eventTypeId, messageHandle);
        }
    }
}