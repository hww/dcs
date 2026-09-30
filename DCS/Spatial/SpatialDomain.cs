using DCS.Core;

namespace DCS.Spatial
{
    /// <summary>
    /// SpatialDomain — контейнер пространственных данных.
    ///
    /// Содержит:
    /// - DynamicSpatial (равномерная сетка для подвижных хостов)
    /// - NameIndex / ArchetypeIndex (индексы для поиска)
    /// - StaticSpatial (опционально — запечённые данные уровня)
    ///
    /// НЕ содержит источников данных и НЕ обновляется сам.
    /// Обновление — задача систем в игре (SpatialUpdateSystem, NameIndexSystem, ...).
    /// </summary>
    public sealed class SpatialDomain
    {
        public int Id { get; }

        public DynamicSpatial DynamicSpatial { get; }
        public NameIndex Names { get; }
        public ArchetypeIndex Archetypes { get; }

        /// <summary>Запечённый spatial (опционально). Может быть null.</summary>
        public SpatialRuntime StaticSpatial { get; set; }

        public SpatialDomain(
            int id,
            int gridWidth = 16,
            int gridHeight = 16,
            int gridDepth = 16,
            float cellSize = 12.5f)
        {
            Id = id;
            DynamicSpatial = new DynamicSpatial(gridWidth, gridHeight, gridDepth, cellSize);
            Names = new NameIndex();
            Archetypes = new ArchetypeIndex();
        }

        public void Clear()
        {
            DynamicSpatial.Clear();
            Names.Clear();
            Archetypes.Clear();
            StaticSpatial?.Clear();
        }
    }
}