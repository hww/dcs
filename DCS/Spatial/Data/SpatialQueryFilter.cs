namespace DCS.Spatial
{
    public struct SpatialQueryFilter
    {
        public bool FilterByType;
        public ESpatialObjectType ObjectType;
        public bool FilterByOwner;
        public ushort OwnerId;

        public static SpatialQueryFilter Any => new SpatialQueryFilter { FilterByType = false, FilterByOwner = false };

        public static SpatialQueryFilter ByType(ESpatialObjectType type)
        {
            return new SpatialQueryFilter { FilterByType = true, ObjectType = type };
        }

        public static SpatialQueryFilter ByOwner(ushort ownerId)
        {
            return new SpatialQueryFilter { FilterByOwner = true, OwnerId = ownerId };
        }
    }
}
