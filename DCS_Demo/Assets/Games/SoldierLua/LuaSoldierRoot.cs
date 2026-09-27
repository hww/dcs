using DCS.Actors;
using DCS.Core;
using DCS.Lua;
using DCS.Spatial;
using UnityEngine;

namespace DCS.SoldierCS
{
    /// <summary>
    /// Root игры SoldierCS.
    ///
    /// Root — это "балансировщик": он решает, в каком порядке и с какой частотой
    /// обновлять системы. Хардкод в Update() — это правильно, потому что порядок
    /// вызовов и аргументы видны глазами, а не собираются динамически.
    ///
    /// Root владеет:
    /// - Domain (ECS-контекст)
    /// - SpatialDomainId (пространственный контекст)
    /// - DCSTime (точка правды по времени; может быть несколько экземпляров)
    /// - подсистемами (обычные C#-классы)
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

        // --- Contexts ---
        public Domain Domain { get; private set; }
        public int SpatialDomainId { get; private set; } = DCS.Core.Domain.NoSpatial;

        // --- Time ---
        /// <summary>Обновляется каждый кадр. Используют обычные системы.</summary>
        public DCSTime TimeFrame { get; private set; }

        /// <summary>Обновляется каждый второй кадр. Используют дорогие системы.</summary>
        public DCSTime TimeHalfFrame { get; private set; }

        /// <summary>Не зависит от TimeScale. Для UI, музыка, метрики.</summary>
        public DCSTime TimeUnscaled { get; private set; }

        // --- Subsystems ---
        // private InputSystem _input;
        // private MovementSystem _movement;
        // private AnimationSystem _animation;
        // private TransformSyncSystem _transformSync;

        // --- Internal ---
        private bool _initialized;
        private int _frameCounter;

        private LuaManager _lua;

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
            TimeHalfFrame = new DCSTime();
            TimeUnscaled = new DCSTime();

            // 2. Domain
            Domain = DomainRegistry.Create("SoldierGame");

            // 3. SpatialDomain
            var posSource = new FastPoolPositionSource(
                ComponentRegistry.GetFastPool<PositionComponent>());
            var nameSource = new FastPoolNameSource(
                ComponentRegistry.GetFastPool<NameComponent>());
            var tagSource = new FastPoolTagSource(
                ComponentRegistry.GetFastPool<TagComponent>());

            SpatialDomainId = SpatialDomainRegistry.Create(
                posSource, nameSource, tagSource,
                gridWidth: 16, gridHeight: 16, gridDepth: 16,
                cellSize: 12.5f, radius: 0.5f);

            Domain.AttachSpatial(SpatialDomainId);

            // 4. Subsystems
            // _input = new InputSystem();
            // _movement = new MovementSystem();
            // _animation = new AnimationSystem();
            // _transformSync = new TransformSyncSystem();
            //
            // _input.Init(Domain);
            // _movement.Init(Domain);
            // _animation.Init(Domain);
            // _transformSync.Init(Domain);
        }

        private void Deinitialize()
        {
            // _input?.Deinit();
            // _movement?.Deinit();
            // _animation?.Deinit();
            // _transformSync?.Deinit();

            if (Domain != null && Domain.HasSpatial)
            {
                SpatialDomainRegistry.Remove(Domain.SpatialDomainId);
                Domain.DetachSpatial();
            }
        }

        private void Update()
        {
            // 1. Time — обновляем экземпляры с их частотой.
            TimeFrame.Update();
            TimeUnscaled.Update();
            if ((_frameCounter & 1) == 0)
                TimeHalfFrame.Update();

            // 2. Ранние системы (ввод и т.п.)
            // _input.Update(Domain, TimeFrame);

            // 3. Логика (каждый кадр)
            // _movement.Update(Domain, TimeFrame);
            // _animation.Update(Domain, TimeFrame);

            // 4. Логика (раз в 2 кадра — балансировка Root'ом)
            // _ai.Update(Domain, TimeHalfFrame);

            // 5. Spatial — после всех, кто двигает.
            if (Domain.HasSpatial &&
                SpatialDomainRegistry.TryGet(Domain.SpatialDomainId, out var spatial))
            {
                spatial.Update();
            }

            // Доставка событий через Lua.
            EventSystem.DeliverAll(Domain, Dispatch);  // новое

            // 6. Синхронизация с Transform.
            // _transformSync.Update(Domain, TimeFrame);

            _frameCounter++;
        }


        private void Dispatch(in SubscriptionNode sub, Host sender, int eventTypeId, Handle messageHandle)
        {
            if (sub.ProcessTypeId == ComponentType<SubscriptionNode>.Id)
            {
                _lua?.CallEventRouter(sender.Id, eventTypeId, messageHandle.Pack());
                return;
            }
            var pool = ComponentRegistry.Pools[sub.ProcessTypeId];
            pool?.SystemDeliver(sub.ProcessHandle.Id, eventTypeId, messageHandle);
        }
    }
}