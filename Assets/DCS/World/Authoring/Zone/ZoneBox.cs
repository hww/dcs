using UnityEngine;

namespace DynamicComponent
{
    public sealed class ZoneBox : BaseProxy
    {
        [SerializeField]
        private Vector3 _size = Vector3.one;

        public Vector3 Size
        {
            get => _size;
            set => _size = value;
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

            Gizmos.DrawWireCube(Vector3.zero, _size);
        }
    }
}