using DCS.Core;

[ComponentPool(16)]
public struct CameraNameComponent : IComponent
{
    public int RosterIndex { get; set; }
    public int NameHash;   // FNV-1a от имени
}