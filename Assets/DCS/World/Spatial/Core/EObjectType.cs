namespace DynamicComponent
{
    /// <summary>
    /// Семантический тип spatial-объекта.
    /// SpatialIndex использует его только для фильтрации запросов.
    /// </summary>
    public enum EObjectType : byte
    {
        ZoneTrigger,
        CombatCover,
        GrapplePoint,
        InteractableNode
    }

    /// <summary>
    /// Точная геометрия runtime-объекта.
    /// Это НЕ spatial proxy.
    /// </summary>
    public enum EGeometryType : byte
    {
        Sphere,
        Box,
        Triangle
    }

}
