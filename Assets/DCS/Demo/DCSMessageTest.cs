public class DCSMessageTest : UnityEngine.MonoBehaviour
{
    private Chain _chain;
    private EventDispatcher _dispatcher;

    private HostHandle _player;
    private HostHandle _monster;

    private void Awake()
    {
        // ГАРАНТИРОВАННЫЙ ПРОГРЕВ ПАМЯТИ: Выделяем все 100 пулов до начала игры
        ComponentRegistry.InitializeAllPools();
    }

    private void Start()
    {
        _chain = new Chain();
        _dispatcher = new EventDispatcher();

        _player = new HostHandle { Id = 1, Generation = 1 };
        _monster = new HostHandle { Id = 2, Generation = 1 };

        // =================================================================
        // ТЕСТ 1: РЕЖИМ ПО ПОДПИСКЕ (Poll) — Опрос условий
        // =================================================================
        // Монстр подписывается на событие: "Хочу знать, когда кто-то зайдет в Зону №55"
        // Вызвать при срабатывании логику колбэка №7
        _dispatcher.Subscribe<LocationEvent>(_monster, callbackId: 7, conditionData: 55);

        // Симулируем, что Игрок физически зашел в Зону 55. 
        // Система триггеров выделяет событие в плотном пуле:
        DynamicComponentSystem.Allocate<LocationEvent>(_player, ref _chain, prius: 55);


        // =================================================================
        // ТЕСТ 2: РЕЖИМ БЕЗ ПОДПИСКИ (Notify) — Вброс в Chain и Пуллинг
        // =================================================================
        // Монстр бьет Игрока. Он принудительно вшивает компонент DamageEvent в Chain Игрока:
        _dispatcher.SendNotifyEvent<DamageEvent>(_player, ref _chain, prius: 45.0f);
    }

    private void Update()
    {
        // 1. В начале кадра запускаем фазу Update (Опрос условий)
        DynamicComponentSystem.UpdateComponents(EUpdateStage.Update, _dispatcher);

        // ... Игровой процесс Unity (Движение, логика, ИИ) ...

        // 2. В конце кадра запускаем PostUpdate (Вызов колбэков и сброс покадровой памяти)
        DynamicComponentSystem.UpdateComponents(EUpdateStage.PostUpdate, _dispatcher);


        // --- РЕЖИМ БЕЗ ПОДПИСКИ (Notify / Пуллинг) ---
        // Логика пуллинга урона остается прежней, она работает идеально
        ComponentHandle hDamage = DynamicComponentSystem.Get<DamageEvent>(_player, _chain);
        if (!hDamage.IsNull)
        {
            ref DamageEvent damageData = ref DynamicComponentSystem.ResolveHandle<DamageEvent>(hDamage);
            UnityEngine.Debug.Log($"<color=orange>[БЕЗ ПОДПИСКИ]</color> Получено повреждений: {damageData.Amount}");
            DynamicComponentSystem.Free<DamageEvent>(_player, ref _chain, ref hDamage);
        }
    }
}
