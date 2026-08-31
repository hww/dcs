using UnityEngine;

// 1. Чистый DOD-компонент для вращения. Никаких интерфейсов.
[ComponentPool(5000)]
public struct RotatorComponent
{
    public float Speed; // Скорость вращения (градусов в секунду)
    public float CurrentAngle; // Текущий угол
}

// 2. Система, которая выполняет конкретную работу над пулом
public static class RotatorSystem
{
    // Обновление всех компонентов вращения за один плоский проход по памяти
    public static void Update(float deltaTime, Chain chain)
    {
        // Мгновенно берем менеджер пула для RotatorComponent
        ComponentManager<RotatorComponent> pool = ComponentRegistry.GetPool<RotatorComponent>();

        // Процессор идет строго линейно от 0 до Partition (только по активным элементам)
        for (int i = 0; i < pool.Partition; i++)
        {
            // Берем прямую ref-ссылку на данные в пуле (без копирования!)
            ref RotatorComponent rotator = ref pool.Components[i];
            
            // Из параллельного массива метаданных за 1 такт узнаем, какому Хосту принадлежит память
            HostHandle host = pool.Hosts[i];

            // Наращиваем угол
            rotator.CurrentAngle += rotator.Speed * deltaTime;

            // --- ДОСТАВКА ДАННЫХ В UNITY ---
            // В реальном движке Insomniac здесь был бы вывод в матрицу трансформации SPU.
            // В Unity мы находим GameObject по его ID, чтобы применить поворот визуально.
            GameObject unityObj = GridGenerator.GetVisualObject(host.Id);
            if (unityObj != null)
            {
                unityObj.transform.rotation = Quaternion.Euler(0, rotator.CurrentAngle, 0);
            }
        }
    }
}
