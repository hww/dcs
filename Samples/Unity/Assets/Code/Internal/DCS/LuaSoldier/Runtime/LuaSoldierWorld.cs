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

        // Оставляем только быструю связку hostId -> Actor для Lua-запросов, если они нужны.
        // Массивы рендеринга _views, _transforms, _animators ПОЛНОСТЬЮ удалены.
        private readonly Dictionary<int, Actor> _hostToActor = new();

        public HostChain Chain => _domain.HostChain;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ComponentRegistry.InitializeAllPools();

            _domain = DomainRegistry.Create("Default");

            GameManager.BindDomain(_domain);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Transform camT = Camera != null ? Camera.transform : transform;

            // Теперь в системы передается только контекст, никакой каши из массивов
            KeyboardInputSystem.Update(_domain.HostChain, Camera);
            MovementSystem.Update(_domain.HostChain, camT, dt);
            AnimationSystem.Update(_domain.HostChain);
            TransformSyncSystem.Update(_domain.HostChain);
        }
    }
}
