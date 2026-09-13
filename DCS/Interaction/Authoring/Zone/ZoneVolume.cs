using UnityEngine;

namespace DCS.Core
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
