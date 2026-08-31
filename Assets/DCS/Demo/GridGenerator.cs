using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [Header("Настройки сетки")]
    public int N = 10; // Размер сетки N x N (итого N^2 объектов)
    public float Spacing = 2.0f;
    public float MaxRotationSpeed = 180f;

    // Глобальная таблица связей
    private Chain _chain;

    // Массив для хранения ссылок на визуальные объекты Unity, 
    // чтобы система вращения могла быстро найти их по Host ID
    private static GameObject[] _visualRegistry;

    private void Start()
    {
        ComponentRegistry.InitializeAllPools();

        // 1. Инициализируем системные таблицы
        _chain = new Chain();
        _visualRegistry = new GameObject[Chain.MaxGameObjects];

        // 2. Генерируем сетку объектов
        int hostIdCounter = 0;

        for (int x = 0; x < N; x++)
        {
            for (int z = 0; z < N; z++)
            {
                if (hostIdCounter >= Chain.MaxGameObjects) break;

                // Создаем обычный примитив Unity (куб) для визуализации
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = new Vector3(x * Spacing, 0, z * Spacing);
                cube.name = $"DCHost_{hostIdCounter}";

                // Создаем абсолютно невесомый HostHandle для нашей DCS системы
                HostHandle host = new HostHandle
                {
                    Id = hostIdCounter,
                    Generation = 1
                };

                // Регистрируем визуальный объект в нашем массиве по его ID
                _visualRegistry[host.Id] = cube;

                // Передаем скорость вращения через Prius (в нашем API это object)
                float randomSpeed = Random.Range(30f, MaxRotationSpeed);

                // МАГИЯ API: Выделяем компонент вращения в пуле за O(1)
                // Система сама создаст пул, пропишет связи в _chain и вернет хэндл
                ComponentHandle rotatorHandle = DynamicComponentSystem.Allocate<RotatorComponent>(host, ref _chain, prius: randomSpeed);

                hostIdCounter++;
            }
        }

        Debug.Log($"<color=green>DCS успешно проверена!</color> Создано объектов: {(N * N).ToString()}. " +
                  $"Активных компонентов в пуле вращения: {ComponentRegistry.GetPool<RotatorComponent>().Partition.ToString()}");
    }

    private void Update()
    {
        // Каждое обновление кадра запускаем нашу DOP-систему.
        // Она линейно идет по плоскому массиву структур, вообще не зная про существование Unity,
        // и только в самом конце применяет углы к визуальным объектам.
        RotatorSystem.Update(Time.deltaTime, _chain);
    }

    // Быстрый доступ к Unity-объекту по Host ID без поиска по сцене
    public static GameObject GetVisualObject(int hostId)
    {
        return _visualRegistry[hostId];
    }
}
