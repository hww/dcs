using UnityEngine;

namespace DCS.Interaction.Authoring
{
    [AddComponentMenu("DCS/Spatial/Shape Box")]
    public sealed class ShapeBox : BaseShape
    {
        [SerializeField]
        private Vector3 _size = Vector3.one;

        public Vector3 Size
        {
            get => _size;
            set => _size = new Vector3(
                Mathf.Max(0.01f, value.x),
                Mathf.Max(0.01f, value.y),
                Mathf.Max(0.01f, value.z));
        }

        protected override void DrawShapeGizmo()
        {
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(Vector3.zero, _size);
            Gizmos.matrix = previous;
        }
    }
}
