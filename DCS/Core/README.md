# Core

**Dynamic Component System — инфраструктура, на которой работает всё остальное.**

Core не знает, про какую игру он. Он даёт механизмы: хосты, компоненты, события, факты, планировщик. Всё, что знает про игру, живёт выше.

---

## Назначение

Core отвечает на вопрос: **«как это работает?»**

Он даёт общий рантайм для:

- динамических компонентов;
- хостов и идентичности;
- хранения компонентов;
- событий;
- фактов;
- планирования обновлений;
- маршалинга и инспекции.

---

## Что Core не содержит

Core не содержит игровых концепций:

- актёров, зон, навигации;
- пространственной геометрии;
- квестов, боя, AI;
- правил мира.

Всё это живёт в `Gameplay`, `World`, `Spatial`, `Navigation`, `Interaction`, `Lua` и использует Core как фундамент.

---

## Правило зависимостей

```text
World ──────────┐
Spatial ────────┤
Gameplay ───────┤
Navigation ─────┤──► Core
Interaction ────┤
Lua ────────────┘
```

Обратной зависимости быть не должно. Core не ссылается на `Gameplay`, `World`, `Spatial`, `Lua`.

---

## Принцип

> **Core даёт механизмы, а не смысл.**

Если фичу можно описать, не зная ничего про игру — она в Core. Если нужно игровое понятие — она выше Core.

---

## Структура

```text
Core/
├── Attributes/     # PoolAttribute, GenerateAttribute
├── Components/     # IComponent, IInitializable
├── Config/         # DcsConfig, HandleConfig
├── Domains/        # Domain, DomainRegistry
├── Events/         # IEvent, EventPool, EventSystem, EventSubscription, TypeChain
│   └── Contracts/  # IEventDispatcher, IMessageReceiver
├── Handles/        # Handle, Host, TypedHandle
├── Hosts/          # HostManager, HostChain, HostData, IHostReference
├── Math/           # Crc32
├── Pools/          # ComponentPool, FastPool, ComponentSparse, FastSparseTable, IComponentPool
├── Registry/       # ComponentRegistry, UpdateScheduler
├── Shared/         # IInspectable
└── Systems/        # ComponentSystem, EUpdateStage
```

---

## Что делает каждая папка

| Папка | Отвечает за |
|---|---|
| `Attributes` | Атрибуты для регистрации пулов и генерации кода |
| `Components` | Базовые контракты компонентов |
| `Config` | Глобальные лимиты и битовые маски |
| `Domains` | Изолированные пространства хостов и компонентов |
| `Events` | Доставка событий между хостами |
| `Handles` | Идентификаторы: Handle, Host, TypedHandle |
| `Hosts` | Управление жизненным циклом хостов и их цепочками |
| `Math` | Хеши и утилиты |
| `Pools` | Хранение компонентов: классический и быстрый пулы |
| `Registry` | Регистрация типов компонентов и порядок обновления |
| `Shared` | Общие интерфейсы без конкретной прописки |
| `Systems` | Контракт обновления систем и фасад `DCSystem` |