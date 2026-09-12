using DynamicComponent.Lua.Bindings;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicComponent
{
    public class ZoneDetectionSystem : MonoBehaviour
    {
        [Header("Performance Settings")]
        [SerializeField]
        private int _updateIntervalFrames = 10;

        // Храним ID зон, внутри которых игрок находился на прошлом тике
        private readonly HashSet<ushort> _previouslyInsideZones = new HashSet<ushort>();

        // Буфер для сбора текущих зон (0 аллокаций в Update)
        private readonly HashSet<ushort> _currentInsideZones = new HashSet<ushort>();

        private void Update()
        {
            // Оптимизация: крутим тяжелую логику строго по интервалу кадров
            if (Time.frameCount % _updateIntervalFrames != 0) return;

            // Получаем позицию игрока (в продакшене — быстрый запрос из пула компонентов)
            Vector3 playerPos = GetPlayerPositionRuntime();

            if (SpatialRuntime.Instance == null) return;

            _currentInsideZones.Clear();

            // Запрашиваем у «глупого» индекса все ID объектов типа ZoneTrigger в текущей точке игрока
            List<ushort> activeZoneIds = new List<ushort>();
            SpatialRuntime.Instance.GetObjectsAtPoint(playerPos, EObjectType.ZoneTrigger, activeZoneIds);

            for (int i = 0; i < activeZoneIds.Count; i++)
            {
                _currentInsideZones.Add(activeZoneIds[i]);
            }

            // 1. Анализируем ВХОД в новые зоны
            foreach (ushort zoneId in _currentInsideZones)
            {
                if (!_previouslyInsideZones.Contains(zoneId))
                {
                    MapBindings.NotifyZoneEvent(zoneId, "OnZoneEntered");
                }
            }

            // 2. Анализируем ВЫХОД из старых зон
            foreach (ushort zoneId in _previouslyInsideZones)
            {
                if (!_currentInsideZones.Contains(zoneId))
                {
                    MapBindings.NotifyZoneEvent(zoneId, "OnZoneExited");
                }
            }

            // 3. Сохраняем состояние для следующего тика
            _previouslyInsideZones.Clear();
            foreach (ushort zoneId in _currentInsideZones)
            {
                _previouslyInsideZones.Add(zoneId);
            }
        }

        private Vector3 GetPlayerPositionRuntime()
        {
            if (Camera.main != null) return Camera.main.transform.position;
            return Vector3.zero;
        }
    }
}
