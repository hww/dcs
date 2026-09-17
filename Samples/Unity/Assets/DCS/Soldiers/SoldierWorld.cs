using DCS.Core;
using DCS.Lua;
using UnityEngine;

namespace DCS.Soldiers
{
    public class SoldierWorld : MonoBehaviour
    {
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

        private HostChain _chain;
        private EventSubscription _subPool;
        private TypeChain _typeChain;

        private GameObject[] _views;
        private Animator[] _animators;
        private Transform[] _transforms;
        private int _viewCount;

        private Host _playerHost;

        void Awake()
        {
            ComponentRegistry.InitializeAllPools();

            _chain = new HostChain();
            _subPool = new EventSubscription(Capacity);
            _typeChain = new TypeChain();

            _views = new GameObject[Capacity];
            _animators = new Animator[Capacity];
            _transforms = new Transform[Capacity];

            LuaManager._globalHostChain = _chain;
            LuaManager._eventSubscriptionPool = _subPool;
            LuaManager._globalTypeChain = _typeChain;

            for (int i = 0; i < SoldierCount; i++)
                SpawnSoldier(i == 0);

        }

        void SpawnSoldier(bool isPlayer)
        {
            if (_viewCount >= Capacity) return;

            int viewId = _viewCount++;

            // 1. GameObject из префаба
            Vector3 pos = SpawnOrigin + new Vector3(viewId * SpawnSpacing, 0f, 0f);
            GameObject go = Instantiate(SoldierPrefab, pos, Quaternion.identity);
            go.name = isPlayer ? "Player" : $"Soldier_{viewId}";

            _views[viewId] = go;
            _transforms[viewId] = go.transform;
            _animators[viewId] = go.GetComponentInChildren<Animator>();

            // 2. Хост
            Host host = HostManager.CreateHost();
            HostManager.LinkHostReference(host, go.GetComponent<IHostReference>());

            // 3. Компоненты — через DCSystem
            DCSystem.ResolveHandle<SoldierTag>(
                DCSystem.Allocate<SoldierTag>(host, _chain));

            DCSystem.ResolveHandle<PositionComponent>(
                DCSystem.Allocate<PositionComponent>(host, _chain)).Value = pos;

            DCSystem.ResolveHandle<VelocityComponent>(
                DCSystem.Allocate<VelocityComponent>(host, _chain)).Value = Vector3.zero;

            DCSystem.ResolveHandle<InputComponent>(
                DCSystem.Allocate<InputComponent>(host, _chain));

            DCSystem.ResolveHandle<CombatStateComponent>(
                DCSystem.Allocate<CombatStateComponent>(host, _chain)).Value = ECombatState.Combat;

            DCSystem.ResolveHandle<LocomotionComponent>(
                DCSystem.Allocate<LocomotionComponent>(host, _chain)).Value = ELocomotion.Idle;

            DCSystem.ResolveHandle<ViewComponent>(
                DCSystem.Allocate<ViewComponent>(host, _chain)).ViewId = viewId;

            if (isPlayer)
            {
                DCSystem.ResolveHandle<PlayerTag>(
                    DCSystem.Allocate<PlayerTag>(host, _chain));
                _playerHost = host;

                if (isPlayer && Camera != null)
                    Camera.Target = _transforms[viewId];
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;

            PlayerInputSystem.Update(_playerHost, _chain, Camera);
            MovementSystem.Update(_chain, Camera.transform, dt);
            SoldierAnimationSystem.Update(_chain, _animators);
            TransformSyncSystem.Update(_chain, _transforms);
        }
    }
}