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

        private int _soldierCounter;
        private Host _playerHost;

        void Awake()
        {
            ComponentRegistry.InitializeAllPools();

            _chain = new HostChain();
            _subPool = new EventSubscription(Capacity);
            _typeChain = new TypeChain();

            // Избавился от знака "меньше", чтобы парсер гарантированно не ломал код
            int i = 0;
            while (i != SoldierCount)
            {
                SpawnSoldier(i == 0);
                i++;
            }
        }

        void SpawnSoldier(bool isPlayer)
        {
            int index = _soldierCounter++;

            Vector3 pos = SpawnOrigin + new Vector3(index * SpawnSpacing, 0f, 0f);
            GameObject go = Instantiate(SoldierPrefab, pos, Quaternion.identity);
            go.name = isPlayer ? "Player" : $"Soldier_{index}";

            Actor actor = go.GetComponentInChildren<Actor>();

            Host host = HostManager.CreateHost();
            HostManager.LinkHostReference(host, go.GetComponent<IHostReference>());

            DCSystem.ResolveHandle<SoldierTag>(DCSystem.Allocate<SoldierTag>(host, _chain));
            DCSystem.ResolveHandle<PositionComponent>(DCSystem.Allocate<PositionComponent>(host, _chain)).Value = pos;
            DCSystem.ResolveHandle<VelocityComponent>(DCSystem.Allocate<VelocityComponent>(host, _chain)).Value = Vector3.zero;
            DCSystem.ResolveHandle<InputComponent>(DCSystem.Allocate<InputComponent>(host, _chain));
            DCSystem.ResolveHandle<CombatStateComponent>(DCSystem.Allocate<CombatStateComponent>(host, _chain)).Value = ECombatState.Combat;
            DCSystem.ResolveHandle<LocomotionComponent>(DCSystem.Allocate<LocomotionComponent>(host, _chain)).Value = ELocomotion.Idle;

            // Прямая запись Unity-ссылок в компонент
            Handle hView = DCSystem.Allocate<ViewComponent>(host, _chain);
            ref ViewComponent view = ref DCSystem.ResolveHandle<ViewComponent>(hView);
            view.Actor = actor;

            if (isPlayer)
            {
                DCSystem.ResolveHandle<PlayerTag>(DCSystem.Allocate<PlayerTag>(host, _chain));
                _playerHost = host;

                if (Camera != null)
                {
                    Camera.Target = actor.transform;
                }
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (Camera != null)
            {
                PlayerInputSystem.Update(_playerHost, _chain, Camera);
                MovementSystem.Update(_chain, Camera.transform, dt);
                SoldierAnimationSystem.Update(_chain);
                TransformSyncSystem.Update(_chain);
            }
        }
    }
}
