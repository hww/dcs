using UnityEngine;

namespace DCS.Interaction.Authoring
{
    [AddComponentMenu("DCS/Spatial/Shape Sphere")]
    public sealed class ShapeSphere : BaseShape
    {
        [SerializeField]
        private float _radius = 1f;

        public float Radius
        {
            get => _radius;
            set => _radius = Mathf.Max(0.01f, value);
        }

        protected override void DrawShapeGizmo()
        {
            float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            scale = Mathf.Max(scale, Mathf.Abs(transform.lossyScale.z));
            Gizmos.DrawWireSphere(transform.position, _radius * scale);
        }
    }
}
