#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DynamicComponent.Packing
{
    public static class WorldPacker
    {
        [MenuItem("DCS Engine/Bake Active Map Assets")]
        public static void BakeActiveSceneMenu()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                Debug.LogError("[WorldPacker] Невозможно запечь карту: активная сцена не валидна.");
                return;
            }

            string mapName = activeScene.name;
            Debug.Log($"<color=orange>[WorldPacker]</color> Начало компиляции конвейера данных для: {mapName}");

            string targetFolder = "Assets/Resources/Maps";
            Directory.CreateDirectory(targetFolder);
            string assetPath = $"{targetFolder}/{mapName}_dataset.asset";

            MapDataset dataset = AssetDatabase.LoadAssetAtPath<MapDataset>(assetPath);
            if (dataset == null)
            {
                dataset = ScriptableObject.CreateInstance<MapDataset>();
                AssetDatabase.CreateAsset(dataset, assetPath);
            }

            // Используем ваш новый метод очистки
            dataset.Clear();
            dataset.MapName = mapName;

            GameObject[] rootObjects = activeScene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                // 1. Компилируем Зоны через обновленный конвейер
                var zones = root.GetComponentsInChildren<Zone>(true);
                foreach (var zone in zones)
                {
                    dataset.Zones.Add(ZoneCompiler.CompileMetadata(zone));
                    ZoneCompiler.CompileGeometry(zone, dataset);
                }

                // 2. Компилируем Спаунеры
                var spawners = root.GetComponentsInChildren<Spawner>(true);
                foreach (var spawner in spawners)
                {
                    dataset.Spawners.Add(new SpawnerRecord
                    {
                        HostId = spawner.Host.Id,
                        Position = spawner.transform.position,
                        EntityClass = spawner.entityClassToSpawn ?? string.Empty
                    });

                    // Спаунер как точка интереса (регистрируем в геометрию)
                    int geometryIndex = dataset.Spheres.Count;
                    dataset.Spheres.Add(new SphereGeometry
                    {
                        Center = spawner.transform.position,
                        Radius = spawner.radius
                    });

                    dataset.SpatialProxies.Add(new SpatialProxy(
                        (ushort)dataset.SpatialProxies.Count,
                        spawner.Host.Id,
                        EObjectType.InteractableNode,
                        new GeometryHandle(EGeometryType.Sphere, geometryIndex),
                        spawner.transform.position - Vector3.one * spawner.radius,
                        spawner.transform.position + Vector3.one * spawner.radius
                    ));
                }

                // 3. Компилируем Локаторы
                var locators = root.GetComponentsInChildren<Locator>(true);
                foreach (var locator in locators)
                {
                    if (locator is Spawner) continue;
                    dataset.Locators.Add(new LocatorRecord
                    {
                        HostId = locator.Host.Id,
                        Position = locator.transform.position,
                        TagsJson = locator.Facts != null ? locator.Facts.ToJson() : "{}"
                    });
                }
            }

            EditorUtility.SetDirty(dataset);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=green>[WorldPacker]</color> Компиляция завершена! Ассет сохранен: {assetPath}\n" +
                      $"Статистика -> Зон: {dataset.Zones.Count}, Спаунеров: {dataset.Spawners.Count}, Прокси в индексе: {dataset.SpatialProxies.Count}");
        }
    }
}
#endif
