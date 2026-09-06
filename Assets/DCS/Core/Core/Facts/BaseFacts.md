# BaseFacts System

## Overview

BaseFacts предлагает две основные реализации системы данных:
- **Статические контейнеры** (`HeroFacts`) - для максимальной производительности
- **Динамические контейнеры** (`JsonDynamicFacts`) - для гибкости и редактирования

## Static Implementation (HeroFacts)

### Особенности
- 🚀 **Максимальная производительность** - прямой доступ к полям
- 🔒 **Типобезопасность** - компилятор проверяет типы
- 📊 **Сериализация Unity** - поля видны в инспекторе
- ⚡ **Нулевые аллокации** - нет boxing/unboxing

### Пример использования

```csharp
public class PlayerController : MonoBehaviour
{
    [SerializeField] private HeroFacts facts;
    
    void Update()
    {
        // Прямой доступ к данным - максимальная производительность
        transform.Translate(Vector3.forward * facts.speed * Time.deltaTime);
        
        if (facts.health <= 0)
        {
            facts.isAlive = false;
        }
    }
}
```

### Структура класса

```csharp
/// <summary>
/// Статическая реализация BaseFacts с явно определенными полями.
/// Оптимальна для производительности и типобезопасности.
/// </summary>
public class HeroFacts : BaseFacts
{
    // Сериализуемые поля (видны в инспекторе Unity)
    public float speed = 5f;
    public int health = 100;
    public string characterName = "Hero";
    public bool isAlive = true;
    public Vector2 position = Vector2.zero;
    public Color characterColor = Color.white;

    // Реализация абстрактных методов BaseFacts...
}
```

## Dynamic Implementation (JsonDynamicFacts)

### Особенности
- 🎨 **JSON-формат** - понятный и редактируемый
- 🔧 **Динамическое добавление** - факты можно добавлять в runtime
- 📝 **Валидация** - невалидный JSON сразу виден
- 🌐 **Совместимость** - стандартный формат данных

### Пример использования

```csharp
public class GameManager : MonoBehaviour
{
    [SerializeField] private JsonDynamicFacts gameSettings;
    
    void Start()
    {
        // Установка значений
        gameSettings.Set("difficulty", "hard");
        gameSettings.Set("player_name", "Player One");
        gameSettings.Set("spawn_rate", 2.5f);
        gameSettings.Set("enable_sound", true);
        gameSettings.Set("start_position", new Vector3(10, 5, 0));
        
        // Получение значений
        string name = gameSettings.Get<string>("player_name");
        float spawnRate = gameSettings.Get<float>("spawn_rate", 1.0f); // с default value
        Vector3 position = gameSettings.Get<Vector3>("start_position");
    }
}
```

### JSON Representation

В инспекторе Unity данные отображаются в формате JSON:

```json
{
  "player_name": "Player One",
  "health": 100,
  "speed": 5.5,
  "is_alive": true,
  "position": [10.0, 5.0],
  "character_color": [1.0, 0.5, 0.0, 1.0]
}
```

### Поддерживаемые типы данных

| Тип | JSON Пример | Описание |
|-----|-------------|----------|
| `bool` | `true`, `false` | Логические значения |
| `int` | `100`, `-5` | Целые числа |
| `float` | `3.14`, `-2.5` | Числа с плавающей точкой |
| `string` | `"Hello"` | Строки (в двойных кавычках) |
| `Vector2` | `[1.0, 2.0]` | 2D векторы |
| `Vector3` | `[1.0, 2.0, 3.0]` | 3D векторы |
| `Color` | `[1.0, 0.5, 0.0, 1.0]` | Цвета (RGBA) |

## Сравнение подходов

### HeroFacts (Статический)
**✅ Преимущества:**
- Максимальная производительность
- Полная типобезопасность
- Автодополнение в IDE
- Легкий рефакторинг

**⚠️ Ограничения:**
- Фиксированная структура
- Требует перекомпиляции для изменений

### JsonDynamicFacts (Динамический)
**✅ Преимущества:**
- Гибкость в runtime
- Легкое редактирование
- Динамическое добавление полей
- Совместимость с другими системами

**⚠️ Ограничения:**
- Небольшие затраты на парсинг
- Меньшая типобезопасность

## Рекомендации по использованию

### Используйте HeroFacts когда:
- Данные известны на этапе разработки
- Требуется максимальная производительность
- Структура данных стабильна
- Нужна полная типобезопасность

### Используйте JsonDynamicFacts когда:
- Данные добавляются динамически
- Нужно редактирование дизайнерами
- Требуется гибкость в runtime
- Важна совместимость с внешними системами

## Пример гибридного использования

```csharp
public class GameEntity : MonoBehaviour
{
    [SerializeField] private HeroFacts staticFacts;     // Статические данные
    [SerializeField] private JsonDynamicFacts dynamicFacts; // Динамические данные
    
    void Start()
    {
        // Быстрый доступ к статическим данным
        float baseSpeed = staticFacts.speed;
        
        // Динамические модификаторы
        float speedMultiplier = dynamicFacts.Get<float>("speed_multiplier", 1.0f);
        
        // Итоговая скорость
        float finalSpeed = baseSpeed * speedMultiplier;
    }
}
```

Эта система предоставляет оптимальный баланс между производительностью и гибкостью для различных сценариев использования в Unity-проектах.
