using DCS.Actors;
using DCS.Core;
using DCS.Lua;
using DCS.Spatial;
using UnityEngine;

namespace DCS.Game.SoldierLua
{
    /// <summary>
    /// Root игры SoldierLua.
    ///
    /// Комбайн. Никаких Awake/Start/OnEnable.
    /// Initialize() вызывается из Ensure() — единственная точка входа.
    /// Deinitialize() вызывается снаружи (при выгрузке уровня), не через OnDestroy.
    ///
    /// Все параметры игры (что спавнить, где, сколько) — в Lua.
    /// Root предоставляет API и обновляет мир.
    /// </summary>
    public sealed class LuaSoldierRoot : MonoBehaviour
    {
        private static LuaSoldierRoot _instance;
        public static LuaSoldierRoot Instance => _instance;

        /// <summary>
        /// Создаёт или возвращает Root.
        /// Аналог main(): один раз создаёт комбайн, запускает Lua.
        /// </summary>
        public static LuaSoldierRoot Ensure()
        {
            if (_instance != null) return _instance;

            var go = new GameObject("[LuaSoldierRoot]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<LuaSoldierRoot>();
            _instance.Initialize();
            return _instance;
        }

        // --- Contexts ---
        public Domain Domain { get; private set; }
        public int SpatialDomainId { get; private set; } = Domain.NoSpatial;
        public DCSTime TimeFrame { get; private set; }

        // --- Subsystems ---
        private LuaManager _lua;
        private SpatialUpdateSystem _spatialUpdate;

        private bool _initialized;
        private bool _initializing;

        /// <summary>
        /// Явная инициализация. Не Awake, не Start.
        /// Вызывается один раз из Ensure.
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            Application.runInBackground = true;
            TimeFrame = new DCSTime();

            Domain = DomainRegistry.Create("SoldierLuaGame");
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

            _lua = new LuaManager();
            _lua.Initialize(LuaManagerConfig.Default);
        }

        private void Start()
        {
            // Точка входа Lua-игры.
            _lua.CallGlobal("DCS_Global_GameBoot");
        }

        /// <summary>
        /// Явная выгрузка. Вызывается снаружи (например, при смене уровня).
        /// Не OnDestroy, не OnDisable.
        /// </summary>
        public void Deinitialize()
        {
            if (!_initialized) return;

            _lua?.Deinitialize();
            _lua = null;

            if (Domain != null && Domain.HasSpatial)
            {
                SpatialDomainRegistry.Remove(Domain.SpatialDomainId);
                Domain.DetachSpatial();
            }

            _spatialUpdate = null;
            Domain = null;
            _initialized = false;

            if (_instance == this)
            {
                _instance = null;
                Destroy(gameObject);
            }
        }

        private void OnDisable()
        {
            Deinitialize();
        }

        /// <summary>
        /// Тик мира. Вызывается Unity автоматически, потому что Root — MonoBehaviour.
        /// Порядок хардкодный.
        /// </summary>
        private void Update()
        {
            if (!_initialized) return;

            TimeFrame.Update();
            _lua?.Update();

            var chain = Domain.HostChain;
            Transform camTransform = Camera.main != null ? Camera.main.transform : null;

            KeyboardInputSystem.Update(chain);
            MovementSystem.Update(chain, camTransform, TimeFrame.DeltaTime);
            AnimationSystem.Update(chain);
            TransformSyncSystem.Update(chain);

            _spatialUpdate.Update();
            EventSystem.DeliverAll(Domain, Dispatch);
        }

        /// <summary>
        /// Доставка событий. Lua-путь или C#-путь.
        /// </summary>
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