using UnityEngine;

namespace DynamicComponent
{
    [RequireComponent(typeof(Collider))]
    public class ZoneVolume : BaseProxy
    {
        public Collider VolumeCollider { get; private set; }

        private void Awake()
        {
            VolumeCollider = GetComponent<Collider>();
            VolumeCollider.isTrigger = true;
        }
    }
}
