using System.Collections.Generic;
using DCS.Core;
using DCS.Lua;
using UnityEngine;

namespace DCS.LuaSoldier
{
    public class LuaSoldierWorld : MonoBehaviour
    {
        public static LuaSoldierWorld Instance { get; private set; }

        [Header("Camera")]
        public LuaSoldierCamera Camera;

        [Header("World")]
        public int Capacity = 100;

        private Domain _domain;
        private EventSubscription _subPool;
        private TypeChain _typeChain;

        // Связка hostId → GameObject (через ViewComponent)
        private readonly Dictionary<int, GameObject> _hostToGo = new();
        private readonly Dictionary<int, int> _hostToViewId = new();

        private GameObject[] _views;
        private Animator[] _animators;
        private Transform[] _transforms;
        private int _viewCount;

        public HostChain Chain => _domain.HostChain;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            ComponentRegistry.InitializeAllPools();

            _domain = DomainRegistry.Create("Default");


            _views = new GameObject[Capacity];
            _animators = new Animator[Capacity];
            _transforms = new Transform[Capacity];

            GameManager.BindDomain(_domain);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Transform camT = Camera != null ? Camera.transform : transform;

            KeyboardInputSystem.Update(_domain.HostChain, Camera);
            MovementSystem.Update(_domain.HostChain, camT, dt);
            AnimationSystem.Update(_domain.HostChain, _animators);
            TransformSyncSystem.Update(_domain.HostChain, _transforms);
        }

        /// <summary>
        /// Вызывается из Lua через DCS_AttachPrefab.
        /// Загружает префаб, инстанциирует, добавляет ViewComponent,
        /// связывает с хостом.
        /// </summary>
        public bool AttachPrefab(Host host, string prefabPath)
        {
            if (_viewCount >= Capacity) return false;

            // Загружаем префаб из Resources
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[LuaSoldierWorld] Префаб не найден в Resources: {prefabPath}");
                return false;
            }

            // Инстанциируем
            GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            go.name = $"Soldier_{host.Id}";

            int viewId = _viewCount++;
            _views[viewId] = go;
            _transforms[viewId] = go.transform;
            _animators[viewId] = go.GetComponentInChildren<Animator>();

            // Связка Host ↔ GameObject через HostManager
            var link = go.GetComponent<IHostReference>();
            if (link != null)
                HostManager.LinkHostReference(host, link);
            else
                Debug.LogWarning($"[LuaSoldierWorld] На префабе {prefabPath} нет IHostReference.");

            // Добавляем ViewComponent — связь с трансформом и аниматором
            Handle hView = DCSystem.Allocate<ViewComponent>(host, _domain.HostChain);
            DCSystem.ResolveHandle<ViewComponent>(hView).ViewId = viewId;

            // Запоминаем связку
            _hostToGo[host.Id] = go;
            _hostToViewId[host.Id] = viewId;

            // Если это игрок — привязываем камеру
            Handle hPlayer = DCSystem.Get<PlayerTag>(host, _domain.HostChain);
            if (!hPlayer.IsNull && Camera != null)
                Camera.Target = go.transform;

            return true;
        }

        /// <summary>Получить GameObject по хосту (для Lua, если нужно).</summary>
        public GameObject GetGameObject(Host host)
        {
            return _hostToGo.TryGetValue(host.Id, out var go) ? go : null;
        }
    }
}