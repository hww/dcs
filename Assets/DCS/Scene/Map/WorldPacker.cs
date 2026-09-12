#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
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

            // Создаем или находим ScriptableObject ассет для этой карты
            string targetFolder = "Assets/Resources/Maps";
            Directory.CreateDirectory(targetFolder);
            string assetPath = $"{targetFolder}/{mapName}_dataset.asset";

            MapDataset dataset = AssetDatabase.LoadAssetAtPath<MapDataset>(assetPath);
            if (dataset == null)
            {
                dataset = ScriptableObject.CreateInstance<MapDataset>();
                AssetDatabase.CreateAsset(dataset, assetPath);
            }

            // Очищаем старые данные
            dataset.MapName = mapName;
            dataset.Zones.Clear();
            dataset.Spawners.Clear();
            dataset.Locators.Clear();
            dataset.SpatialShapes.Clear();

            // Счетчики
            ushort globalShapeIdCounter = 0;

            // Собираем все корневые объекты сцены
            GameObject[] rootObjects = activeScene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                // 1. Компилируем Зоны
                var zones = root.GetComponentsInChildren<Zone>(true);
                foreach (var zone in zones)
                {
                    dataset.Zones.Add(ZoneCompiler.CompileMetadata(zone));
                    ZoneCompiler.CompileGeometry(zone, ref globalShapeIdCounter, dataset.SpatialShapes);
                }

                // 2. Компилируем Спаунеры (вынесено в простую сухую структуру)
                var spawners = root.GetComponentsInChildren<Spawner>(true);
                foreach (var spawner in spawners)
                {
                    dataset.Spawners.Add(new SpawnerRecord
                    {
                        HostId = spawner.Host.Id,
                        Position = spawner.transform.position,
                        EntityClass = spawner.entityClassToSpawn ?? string.Empty
                    });

                    // Если спаунеру нужна регистрация в Spatial Index (например, чтобы пули знали, где респаун-зона)
                    // мы можем скомпилировать его радиус как сферу:
                    globalShapeIdCounter++;
                    dataset.SpatialShapes.Add(new SpatialShapeRecord
                    {
                        ShapeId = globalShapeIdCounter,
                        OwnerId = spawner.Host.Id,
                        ObjectType = EObjectType.InteractableNode,
                        GeomType = EGeometryType.Sphere,
                        RawData0 = spawner.transform.position,
                        RawData1 = new Vector3(spawner.radius, 0, 0),
                        AABBMin = spawner.transform.position - Vector3.one * spawner.radius,
                        AABBMax = spawner.transform.position + Vector3.one * spawner.radius
                    });
                }

                // 3. Компилируем Локаторы
                var locators = root.GetComponentsInChildren<Locator>(true);
                foreach (var locator in locators)
                {
                    if (locator is Spawner) continue; // Пропускаем, они ушли в свою таблицу
                    dataset.Locators.Add(new LocatorRecord
                    {
                        HostId = locator.Host.Id,
                        Position = locator.transform.position,
                        TagsJson = locator.Facts != null ? locator.Facts.ToJson() : "{}"
                    });
                }
            }

            // Сохраняем ассет средствами Unity. Движок сам запишет всё в оптимальный бинарник!
            EditorUtility.SetDirty(dataset);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=green>[WorldPacker]</color> Компиляция завершена! Ассет сохранен: {assetPath}\n" +
                      $"Статистика -> Зон: {dataset.Zones.Count}, Спаунеров: {dataset.Spawners.Count}, Геометрических примитивов в индексе: {dataset.SpatialShapes.Count}");
        }
    }
}
#endif
