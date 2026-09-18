using DCS.Core;

namespace DCS.Core
{
    [ComponentPool(16)]
    public struct CameraNameComponent : IComponent
    {
        public int RosterIndex { get; set; }
        public string Name;   // FNV-1a от имени
    }
}