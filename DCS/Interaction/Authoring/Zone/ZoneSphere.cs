using UnityEngine;

namespace DCS.Core
{
    public sealed class ZoneSphere : BaseProxy
    {
        [SerializeField]
        private float _radius = 1f;

        public float Radius
        {
            get => _radius;
            set => _radius = Mathf.Max(0.01f, value);
        }

        public Vector3 Center => transform.position;
        public Quaternion Rotation => transform.rotation;

        private void OnDrawGizmos()
        {
            Gizmos.matrix = Matrix4x4.TRS(
                transform.position,
                transform.rotation,
                Vector3.one
            );

            Gizmos.DrawWireSphere(Vector3.zero, _radius);
        }
    }
}