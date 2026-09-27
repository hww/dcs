using System.Collections.Generic;

namespace DCS.Spatial
{
    /// <summary>
    /// Реестр SpatialDomain-ов. Он же — фабрика (Create).
    ///
    /// Один SpatialDomain может быть привязан к нескольким Domain через
    /// Domain.AttachSpatial(id). Один Domain может не иметь spatial вообще.
    /// </summary>
    public static class SpatialDomainRegistry
    {
        private static readonly Dictionary<int, SpatialDomain> _byId = new();
        private static int _nextId = 0;

        /// <summary>
        /// Создаёт новый SpatialDomain с указанными источниками.
        /// Возвращает его id — привяжи к Domain через Domain.AttachSpatial(id).
        /// </summary>
        public static int Create(
            IPositionSource positions,
            INameSource names,
            ITagSource tags,
            int gridWidth = 16,
            int gridHeight = 16,
            int gridDepth = 16,
            float cellSize = 12.5f,
            float radius = 0.5f)
        {
            int id = _nextId++;
            var domain = new SpatialDomain(
                id,
                positions,
                names,
                tags,
                gridWidth,
                gridHeight,
                gridDepth,
                cellSize,
                radius);
            _byId[id] = domain;
            return id;
        }

        public static SpatialDomain Get(int spatialDomainId)
            => _byId.TryGetValue(spatialDomainId, out var s) ? s : null;

        public static bool TryGet(int spatialDomainId, out SpatialDomain spatial)
            => _byId.TryGetValue(spatialDomainId, out spatial);

        public static void Remove(int spatialDomainId)
            => _byId.Remove(spatialDomainId);

        public static int Count => _byId.Count;

        public static void Clear()
        {
            _byId.Clear();
            _nextId = 0;
        }
    }
}