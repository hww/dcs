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
public struct DummyStateComponent : IComponentData
{
    public float TimeInState;
}

[ComponentPool(100)]
public struct MonsterBrainProcess : IComponentData, IDcsMessageReceiver
{
    public ComponentHandle CurrentStateHandle; // Хэндл на текущее состояние в его пуле

    // ВАШЕ СИСТЕМНОЕ СОГЛАШЕНИЕ: Сообщение всегда прилетает Процессу, 
    // а Процесс транслирует его в текущее Состояние!
    public void ReceiveMessage(MessageHandle msgHandle)
    {
        // Проверяем, какой тип сообщения прилетел через кастомный свитч-кейс диспетчер (Вариант Б)
        if (msgHandle.TypeId == ComponentType<LocationEvent>.Id)
        {
            // Извлекаем чистые данные ивента без боксинга по хэндлу из пула
            ref LocationEvent locEvent = ref DynamicComponentSystem.ResolveHandle<LocationEvent>(msgHandle);
            
            UnityEngine.Debug.Log($"<color=green>[ПРОЦЕСС АВТОМАТА]</color> Доставлено! " +
                                  $"Монстр узнал, что в зоне {locEvent.ZoneId} произошло движение. " +
                                  $"Переключаем хэндл состояния...");
        }
    }
}

// =========================================================================
// ПЕРЕРАБОТАННЫЙ ТЕСТОВЫЙ СЦЕНАРИЙ
// =========================================================================
public class DCSMessageTest : MonoBehaviour
{
    private Chain _chain;
    private SubscriptionManager _subManager;

    private HostHandle _player;
    private HostHandle _monster;
    
    private ComponentHandle _monsterProcessHandle;

    private void Awake()
    {
        // ГАРАНТИРОВАННЫЙ ПРОГРЕВ ПАМЯТИ: Выделяем все пулы до начала игры
        ComponentRegistry.InitializeAllPools();
    }

    private void Start()
    {
        _chain = new Chain();
        
        // Теперь подписками заведует выделенный SubscriptionManager (TODO 6)
        _subManager = new SubscriptionManager(1000);

        // Инициализируем «паспорта» игровых объектов
        _player = new HostHandle { Id = 1, Generation = 1 };
        _monster = new HostHandle { Id = 2, Generation = 1 };

        // Рождаем персистентный Процесс-Автомат для Монстра в его пуле памяти
        _monsterProcessHandle = DynamicComponentSystem.Allocate<MonsterBrainProcess>(_monster, _chain);

        // =================================================================
        // ТЕСТ 1: РЕЖИМ ПО ПОДПИСКЕ (Poll) — Опрос пространств имен
        // =================================================================
        // Монстр подписывается на ТИП сообщения LocationEvent.
        // Он передает хэндл своего Процесса-Слушателя и битфилд-канал ZoneАlert (0 мусора!)
        _subManager.AllocateSubscription<LocationEvent, MonsterBrainProcess>(
            _monster, 
            _monsterProcessHandle, 
            GameChannels.TriggerZone, 
            _chain
        );

        // Симулируем, что Игрок зашел в Зону 55. 
        // Система триггеров выделяет событие в пуле, привязывает к Игроку и маркирует битфилдом канала!
        ComponentHandle hLoc = DynamicComponentSystem.Allocate<LocationEvent>(_player, _chain);
        ref LocationEvent locData = ref DynamicComponentSystem.ResolveHandle<LocationEvent>(hLoc);
        locData.ZoneId = 55;
        locData.NamespaceMask = GameChannels.TriggerZone; // Помечаем событие битовой маской канала


        // =================================================================
        // ТЕСТ 2: РЕЖИМ БЕЗ ПОДПИСКИ (Notify) — Точечный Пуллинг по Коэну
        // =================================================================
        // Монстр бьет Игрока. Выделяем ивент урона, привязываем его к Игроку за O(1) через Chain.
        // Никаких диспетчеров для Notify-вспышек больше нет!
        ComponentHandle hDmgAlloc = DynamicComponentSystem.Allocate<DamageEvent>(_player, _chain);
        ref DamageEvent dmgAllocData = ref DynamicComponentSystem.ResolveHandle<DamageEvent>(hDmgAlloc);
        dmgAllocData.Amount = 45f;
    }

    private void Update()
    {
        // 1. В начале кадра система фильтрует покадровые пулы событий по битовым маскам (Phase A: Gather)
        EventSystem.PollEvents<LocationEvent>(_subManager);
        
        // Если в игре появится другое событие, программист просто добавит строку:
        // EventSystem.PollEvents&lt;DamageEvent&gt;(_subManager);

        // ... Игровой процесс (Обсчет физики, коллизий, ИИ) ...

        // 2. Доставка событий Процессам-Получателям без мусора (Phase B: Deliver)
        EventSystem.DeliverEvents(_chain);


        // --- РЕЖИМ БЕЗ ПОДПИСКИ (Notify / Чистокровный пуллинг функции Get по Коэну) ---
        // Система здоровья (HealthSystem) в свою фазу точечно забирает урон из цепочки Игрока
        ComponentHandle hDamage = DynamicComponentSystem.Get<DamageEvent>(_player, _chain);
        if (!hDamage.IsNull)
        {
            ref DamageEvent damageData = ref DynamicComponentSystem.ResolveHandle<DamageEvent>(hDamage);
            
            UnityEngine.Debug.Log($"<color=orange>[БЕЗ ПОДПИСКИ (Get)]</color> Система здоровья Игрока " +
                                  $"точечно извлекла урон из Chain: {damageData.Amount}");
            
            // Забираем урон и мгновенно очищаем ноду из цепочки, возвращая во FreeList
            DynamicComponentSystem.Free<DamageEvent>(_player, _chain, ref hDamage);
        }

        // В самом конце кадра очищаем покадровые EVE-пулы (Phase C: Clear)
        ComponentRegistry.GetPool<LocationEvent>().ClearFramePool();
        ComponentRegistry.GetPool<DamageEvent>().ClearFramePool();
    }
}