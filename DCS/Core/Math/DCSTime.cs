using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Точка правды по времени.
    /// Читает UnityEngine.Time один раз за вызов Update.
    ///
    /// Класс, а не struct — чтобы можно было:
    /// - хранить ссылку в системе,
    /// - наследовать и расширять,
    /// - передавать без копирования.
    ///
    /// В Root может быть несколько экземпляров для разных частот обновления.
    /// Root — это и есть "балансировщик": он решает, кого вызывать каждый кадр,
    /// кого — раз в два, и какой DCSTime передавать.
    /// </summary>
    public class DCSTime
    {
        // --- Абсолютные ---
        /// <summary>Монотонное игровое время с учётом TimeScale.</summary>
        public double AbsoluteTime;

        /// <summary>Монотонное реальное время без масштаба.</summary>
        public double UnscaledTime;

        /// <summary>Сколько раз вызывался Update.</summary>
        public long FrameCount;

        // --- Дельты ---
        /// <summary>Дельта игрового времени за последний Update.</summary>
        public float DeltaTime;

        /// <summary>Дельта реального времени без масштаба.</summary>
        public float UnscaledDeltaTime;

        /// <summary>Фиксированный шаг Unity (для физики).</summary>
        public float FixedDeltaTime;

        // --- Масштаб ---
        /// <summary>Глобальный масштаб времени. 1 = норма, 0.5 = слоу-мо, 0 = пауза.</summary>
        public float TimeScale = 1f;

        // --- Внутреннее ---
        private double _lastUnscaled;
        private bool _hasLast;

        /// <summary>
        /// Обновляет время. Вызывать один раз в кадр — из Root.
        /// Каждый экземпляр DCSTime в Root обновляется со своей частотой.
        /// </summary>
        public void Update()
        {
            double now = UnityEngine.Time.unscaledTimeAsDouble;

            if (!_hasLast)
            {
                _lastUnscaled = now;
                _hasLast = true;
                UnscaledDeltaTime = 0f;
            }
            else
            {
                UnscaledDeltaTime = (float)(now - _lastUnscaled);
                _lastUnscaled = now;
            }

            DeltaTime = UnscaledDeltaTime * TimeScale;
            UnscaledTime = now;
            AbsoluteTime += DeltaTime;
            FixedDeltaTime = UnityEngine.Time.fixedDeltaTime;
            FrameCount++;
        }

        /// <summary>
        /// Сброс в исходное состояние. Полезно при перезапуске уровня.
        /// </summary>
        public void Reset()
        {
            _hasLast = false;
            AbsoluteTime = 0d;
            UnscaledTime = 0d;
            FrameCount = 0;
            DeltaTime = 0f;
            UnscaledDeltaTime = 0f;
        }
    }
}