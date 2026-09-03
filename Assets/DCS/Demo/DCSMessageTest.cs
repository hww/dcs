using UnityEngine;

// =========================================================================
// ДЕКЛАРАЦИЯ ГЕЙМПЛЕЙНЫХ КАНАЛОВ (БИТФИЛДЫ В ИГРЕ)
// =========================================================================
public static class GameChannels
{
    public const uint None       = 0;
    public const uint General    = 1 << 0; // 1
    public const uint Combat     = 1 << 1; // 2
    public const uint TriggerZone = 1 << 2; // 4
}

// =========================================================================
// ПОКАДРОВОЕ СОБЫТИЕ УРОНА (РЕАЛИЗУЕТ МАРКЕР ЯДРА ДЛЯ АВТО-ОЧИСТКИ)
// =========================================================================
[MessagePool(500)]
public struct DamageEvent : IEventData
{
    public float Amount;
    public uint NamespaceMask { get; set; } // Реализация контракта ядра
}

[MessagePool(100)]
public struct LocationEvent : IEventData 
{
    public int ZoneId;
    public uint NamespaceMask { get; set; } // Реализация контракта ядра
}

// =========================================================================
// ТЕСТОВЫЕ КОМПОНЕНТЫ И ИВЕНТЫ (РЕАЛИЗУЮТ МАРКЕРЫ ЯДРА)
// =========================================================================
[ComponentPool(100)]
public struct DummyStateComponent : IDcsComponent
{
    public float TimeInState;

    public int RosterIndex { get; set; }
}

[ComponentPool(100)]
public struct MonsterBrainProcess : IDcsComponent, IDcsMessageReceiver
{
    public DcsHandle CurrentStateHandle; // Хэндл на текущее состояние в его пуле

    public int RosterIndex { get; set; }

    // ВАШЕ СИСТЕМНОЕ СОГЛАШЕНИЕ: Сообщение всегда прилетает Процессу, 
    // а Процесс транслирует его в текущее Состояние!
    public void ReceiveMessage(int msgTypeId, DcsHandle msgHandle)
    {
        // Проверяем, какой тип сообщения прилетел через кастомный свитч-кейс диспетчер (Вариант Б)
        if (msgTypeId == ComponentType<LocationEvent>.Id)
        {
            // Извлекаем чистые данные ивента без боксинга по хэндлу из пула
            ref LocationEvent locEvent = ref DynamicComponentSystem.ResolveHandle<LocationEvent>(msgHandle);
            
            UnityEngine.Debug.Log($"<color=green>[ПРОЦЕСС АВТОМАТА]</color> Доставлено! " +
                                  $"Монстр узнал, что в зоне {locEvent.ZoneId} произошло движение.");
        }
    }
}

// =========================================================================
// ПЕРЕРАБОТАННЫЙ ТЕСТОВЫЙ СЦЕНАРИЙ
// =========================================================================
public class DCSMessageTest : MonoBehaviour
{
    private HostChainManager _hostChain;
    private TypeChainManager _typeChain;
    private SubscriptionManager _subManager;

    private HostHandle _player;
    private HostHandle _monster;
    
    private DcsHandle _monsterProcessHandle;

    private void Awake()
    {
        // ГАРАНТИРОВАННЫЙ ПРОГРЕВ ПАМЯТИ: Выделяем все пулы до начала игры
        ComponentRegistry.InitializeAllPools();
    }

    private void Start()
    {
        _hostChain = new HostChainManager();
        _typeChain = new TypeChainManager(); // Выделяем память под менеджер цепочек типов
        _subManager = new SubscriptionManager(1000);

        // Инициализируем «паспорта» игровых объектов (в реальной игре ID берутся из HostManager)
        _player = new HostHandle { Id = 1, Generation = 1 };
        _monster = new HostHandle { Id = 2, Generation = 1 };

        // ИСПРАВЛЕНИЕ: Чтобы HostChainManager корректно работал с жестко заданными ID, 
        // инициализируем слоты в глобальном пуле HostManager (иначе IsValid вернет false)
        HostManager.GlobalHosts[_player.Id] = new DCHost { Id = _player.Id, Generation = _player.Generation, FirstComponent = -1 };
        HostManager.GlobalHosts[_monster.Id] = new DCHost { Id = _monster.Id, Generation = _monster.Generation, FirstComponent = -1 };

        // Рождаем персистентный Процесс-Автомат для Монстра в его пуле памяти
        _monsterProcessHandle = DynamicComponentSystem.Allocate<MonsterBrainProcess>(_monster, _hostChain);

        // =================================================================
        // ТЕСТ 1: РЕЖИМ ПО ПОДПИСКЕ (Poll) — Опрос пространств имен
        // =================================================================
        // Монстр подписывается на ТИП сообщения LocationEvent.
         _subManager.AllocateSubscription<LocationEvent, MonsterBrainProcess>(
            _monster, 
            _monsterProcessHandle, 
            GameChannels.TriggerZone, 
            _hostChain,
            _typeChain
        );   

        // Симулируем, что Игрок зашел в Зону 55. 
        DcsHandle hLoc = DynamicComponentSystem.Allocate<LocationEvent>(_player, _hostChain);
        ref LocationEvent locData = ref DynamicComponentSystem.ResolveHandle<LocationEvent>(hLoc);
        locData.ZoneId = 55;
        locData.NamespaceMask = GameChannels.TriggerZone; // Помечаем событие битовой маской канала

        // =================================================================
        // ТЕСТ 2: РЕЖИМ БЕЗ ПОДПИСКИ (Notify) — Точечный Пуллинг по Коэну
        // =================================================================
        // Монстр бьет Игрока. Выделяем ивент урона, привязываем его к Игроку за O(1) через Chain.
        DcsHandle hDmgAlloc = DynamicComponentSystem.Allocate<DamageEvent>(_player, _hostChain);
        ref DamageEvent dmgAllocData = ref DynamicComponentSystem.ResolveHandle<DamageEvent>(hDmgAlloc);
        dmgAllocData.Amount = 45f;
    }

    private void Update()
    {
        // ИСПРАВЛЕНО: Переданы верные приватные поля класса с нижним подчеркиванием
        // 1. В начале кадра система фильтрует покадровые пулы событий по битовым маскам и осуществляет доставку
        DynamicComponentSystem.UpdateComponents(EUpdateStage.Update, _subManager, _typeChain, _hostChain);
        
        // ... Игровой процесс (Обсчет физики, коллизий, ИИ) ...

        // --- РЕЖИМ БЕЗ ПОДПИСКИ (Notify / Чистокровный пуллинг функции Get по Коэну) ---
        // Система здоровья (HealthSystem) в свою фазу точечно забирает урон из цепочки Игрока
        DcsHandle hDamage = DynamicComponentSystem.Get<DamageEvent>(_player, _hostChain);
        if (!hDamage.IsNull)
        {
            ref DamageEvent damageData = ref DynamicComponentSystem.ResolveHandle<DamageEvent>(hDamage);
            
            UnityEngine.Debug.Log($"<color=orange>[БЕЗ ПОДПИСКИ (Get)]</color> Система здоровья Игрока " +
                                  $"точечно извлекла урон из Chain: {damageData.Amount}");
            
            // Забираем урон и мгновенно очищаем ноду из цепочки, возвращая во FreeList
            DynamicComponentSystem.Free<DamageEvent>(_player, _hostChain, ref hDamage);
        }

        // ИСПРАВЛЕНО: Вместо ручного Clear пулов вызываем PostUpdate фазу ядра, которая сама очистит все покадровые ивенты
        DynamicComponentSystem.UpdateComponents(EUpdateStage.PostUpdate, _subManager, _typeChain, _hostChain);
    }
}
