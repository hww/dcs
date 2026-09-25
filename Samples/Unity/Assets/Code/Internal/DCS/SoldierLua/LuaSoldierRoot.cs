using DCS.Core;
using DCS.Lua;
using UnityEngine;

namespace DCS.LuaSoldier
{
    public sealed class LuaSoldierRoot : MonoBehaviour
    {
        private static LuaSoldierRoot _instance;
        public static LuaSoldierRoot Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[LuaSoldierRoot]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<LuaSoldierRoot>();
                    _instance.Initialize();
                }
                return _instance;
            }
        }

        [Header("Domain")]
        public string DomainName = "Default";

        private Domain _domain;
        public Domain Domain => _domain;
        private bool _gameBooted;

        private void Initialize()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Пулы — уже созданы LuaManager.Awake, но на всякий случай
            ComponentRegistry.InitializeAllPools();
            DomainRegistry.Create("Default");
            DomainRegistry.Create("GameWorld");
            _domain = DomainRegistry.Get(DomainName) ?? DomainRegistry.Create(DomainName);
        }

        private void Update()
        {
            // 1. GameBoot — один раз, после загрузки сцены
            if (!_gameBooted && LuaManager.Instance != null)
            {
                _gameBooted = true;
                LuaManager.Instance.CallGlobal("DCS_Global_GameBoot");
            }

            // 2. C#-системы
            float dt = Time.deltaTime;
            if (_domain != null && _domain.HostChain != null)
            {
                Transform camT = Camera.main != null ? Camera.main.transform : null;
                KeyboardInputSystem.Update(_domain.HostChain);
                MovementSystem.Update(_domain.HostChain, camT, dt);
                AnimationSystem.Update(_domain.HostChain);
                TransformSyncSystem.Update(_domain.HostChain);
            }

            // 3. Lua-тик
            LuaManager.Instance?.CallGlobal("DCS_Global_FrameUpdate");
        }
    }
}