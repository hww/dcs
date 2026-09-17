using UnityEngine;
using DCS.Core;

// =========================================================================
//  ДЕКЛАРАЦИЯ ГЕЙМПЛЕЙНЫХ КАНАЛОВ (БИТФИЛДЫ)
// =========================================================================

public static class GameChannels
{
    public const uint None = 0;
    public const uint General = 1 << 0;
    public const uint Combat = 1 << 1;
    public const uint TriggerZone = 1 << 2;
}

// =========================================================================
//  СОБЫТИЯ (РЕАЛИЗУЮТ IEvent ДЛЯ АВТО-ОЧИСТКИ)
// =========================================================================

[MessagePool(500)]
public struct DamageEvent : IEvent
{
    public float Amount;
    public int RosterIndex { get; set; }
    public uint NamespaceMask { get; set; }
}

[MessagePool(100)]
public struct LocationEvent : IEvent
{
    public int ZoneId;
    public int RosterIndex { get; set; }
    public uint NamespaceMask { get; set; }
}

// =========================================================================
//  КОМПОНЕНТЫ
// =========================================================================

[ComponentPool(100)]
public struct DummyStateComponent : IComponent
{
    public float TimeInState;
    public int RosterIndex { get; set; }
}

[ComponentPool(100)]
public struct MonsterBrainProcess : IComponent, IMessageReceiver
{
    public Handle CurrentStateHandle;
    public int RosterIndex { get; set; }

    public void ReceiveMessage(int msgTypeId, Handle msgHandle)
    {
        if (msgTypeId == ComponentType<LocationEvent>.Id)
        {
            ref LocationEvent locEvent = ref DCSystem.ResolveHandle<LocationEvent>(msgHandle);
            Debug.Log($"<color=green>[ПРОЦЕСС АВТОМАТА]</color> Монстр узнал, что в зоне {locEvent.ZoneId} произошло движение.");
        }
    }
}

// =========================================================================
//  ТЕСТ
// =========================================================================

public class DCSMessageTest : MonoBehaviour
{
    private HostChain _hostChain;
    private TypeChain _typeChain;
    private EventSubscription _subManager;

    private Host _player;
    private Host _monster;

    private Handle _monsterProcessHandle;
    private UpdateScheduler _updateScheduler;

    private void Awake()
    {
        // Идемпотентно. Внутри ядра — флаг _initialized.
        ComponentRegistry.InitializeAllPools();
    }

    private void Start()
    {
        _hostChain = new HostChain();
        _typeChain = new TypeChain();
        _subManager = new EventSubscription(1000);

        // Создаём хосты через HostManager (а не хардкодом Id).
        _player = HostManager.CreateHost();
        _monster = HostManager.CreateHost();

        // Персистентный Процесс для Монстра.
        _monsterProcessHandle = DCSystem.Allocate<MonsterBrainProcess>(_monster, _hostChain);

        // =================================================================
        //  ТЕСТ 1: подписка на LocationEvent с маской TriggerZone
        // =================================================================
        _subManager.AllocateSubscription<LocationEvent, MonsterBrainProcess>(
            _monster,
            _monsterProcessHandle,
            GameChannels.TriggerZone,
            _hostChain,
            _typeChain);

        // Игрок заходит в Зону 55.
        Handle hLoc = DCSystem.Allocate<LocationEvent>(_player, _hostChain);
        ref LocationEvent locData = ref DCSystem.ResolveHandle<LocationEvent>(hLoc);
        locData.ZoneId = 55;
        locData.NamespaceMask = GameChannels.TriggerZone;

        // =================================================================
        //  ТЕСТ 2: событие без подписки (чистый polling)
        // =================================================================
        Handle hDmgAlloc = DCSystem.Allocate<DamageEvent>(_player, _hostChain);
        ref DamageEvent dmgAllocData = ref DCSystem.ResolveHandle<DamageEvent>(hDmgAlloc);
        dmgAllocData.Amount = 45f;

        _updateScheduler = new UpdateScheduler();
    }

    private void Update()
    {
        // Update-фаза: Poll + Deliver.
        DCSystem.UpdateComponents(
            EUpdateStage.Update,
            _updateScheduler,
            _subManager,
            _typeChain,
            _hostChain);

        // Точечный polling: HealthSystem забирает урон из цепочки Игрока.
        Handle hDamage = DCSystem.Get<DamageEvent>(_player, _hostChain);
        if (!hDamage.IsNull)
        {
            ref DamageEvent damageData = ref DCSystem.ResolveHandle<DamageEvent>(hDamage);
            Debug.Log($"<color=orange>[БЕЗ ПОДПИСКИ (Get)]</color> Система здоровья Игрока извлекла урон: {damageData.Amount}");
            DCSystem.Free<DamageEvent>(_player, _hostChain, ref hDamage);
        }

        // PostUpdate-фаза: ClearFramePool по всем event-пулам.
        DCSystem.UpdateComponents(
            EUpdateStage.PostUpdate,
            _updateScheduler,
            _subManager,
            _typeChain,
            _hostChain);
    }
}