using DCS.Interaction.Authoring;
using UnityEngine;

namespace DCS.Navigation.Authoring
{
    public enum NavigationSurfaceType : byte
    {
        Walkable = 0,
        StealthGrass = 1,
        Avoid = 2
    }

    [AddComponentMenu("DCS/Navigation/Navigation Surface")]
    public sealed class NavigationSurface : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private bool _enabled = true;
        [SerializeField] private NavigationSurfaceType _surfaceType = NavigationSurfaceType.Walkable;
        [SerializeField] private int _cost = 1;
        [SerializeField] private BaseShape _shape;
        [SerializeField] private string[] _tags;

        public string Key { get => _key; set => _key = value; }
        public bool Enabled { get => _enabled; set => _enabled = value; }
        public NavigationSurfaceType SurfaceType { get => _surfaceType; set => _surfaceType = value; }
        public int Cost { get => _cost; set => _cost = Mathf.Max(0, value); }
        public BaseShape Shape => _shape;
        public string[] Tags { get => _tags; set => _tags = value; }
    }
}
