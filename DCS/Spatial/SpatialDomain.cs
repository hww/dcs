using DCS.Core;

namespace DCS.Spatial
{
    /// <summary>
    /// SpatialDomain — пространственный контекст, привязанный к одному или нескольким Domain.
    ///
    /// Содержит:
    /// - DynamicSpatial (равномерная сетка для подвижных хостов)
    /// - DynamicSpatialSystem (обновление spatial из источников)
    /// - NameIndex / TagIndex (индексы для поиска)
    /// - StaticSpatial (опционально — запечённые данные уровня)
    ///
    /// Не знает, откуда берутся данные. Источники передаёт игра.
    /// </summary>
    public sealed class SpatialDomain
    {
        public int Id { get; }

        public DynamicSpatial DynamicSpatial { get; }
        public DynamicSpatialSystem System { get; }
        public NameIndex Names { get; }
        public TagIndex Tags { get; }

        /// <summary>Запечённый spatial (опционально). Может быть null.</summary>
        public SpatialRuntime StaticSpatial { get; set; }

        public SpatialDomain(
            int id,
            IPositionSource positions,
            INameSource names,
            ITagSource tags,
            int gridWidth = 16,
            int gridHeight = 16,
            int gridDepth = 16,
            float cellSize = 12.5f,
            float radius = 0.5f)
        {
            Id = id;
            DynamicSpatial = new DynamicSpatial(gridWidth, gridHeight, gridDepth, cellSize);
            Names = new NameIndex();
            Tags = new TagIndex();
            System = new DynamicSpatialSystem(
                DynamicSpatial,
                Names,
                Tags,
                positions,
                names,
                tags,
                radius);
        }

        /// <summary>Обновляет dynamic spatial и индексы.</summary>
        public void Update()
        {
            System.Update();
        }

        /// <summary>Очищает dynamic spatial и индексы. StaticSpatial не трогает.</summary>
        public void Clear()
        {
            System.Clear();
        }
    }
}