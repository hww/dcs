using UnityEngine;

namespace DCS.Interaction.Authoring
{
    [AddComponentMenu("DCS/Spatial/Shape Cylinder")]
    public sealed class ShapeCylinder : BaseShape
    {
        [SerializeField]
        private float _radius = 1f;

        [SerializeField]
        private float _height = 2f;

        public float Radius
        {
            get => _radius;
            set => _radius = Mathf.Max(0.01f, value);
        }

        public float Height
        {
            get => _height;
            set => _height = Mathf.Max(0.01f, value);
        }

        protected override void DrawShapeGizmo()
        {
            DrawWireCylinder(transform.position, transform.rotation, _radius, _height, transform.lossyScale);
        }

        private static void DrawWireCylinder(
            Vector3 center,
            Quaternion rotation,
            float radius,
            float height,
            Vector3 scale)
        {
            float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float heightScale = Mathf.Abs(scale.y);

            float r = radius * radiusScale;
            float h = height * heightScale;
            float halfHeight = h * 0.5f;

            Vector3 up = rotation * Vector3.up;
            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;

            const int segments = 32;
            Vector3 previousBottom = center + up * -halfHeight + right * r;
            Vector3 previousTop = center + up * halfHeight + right * r;

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 radial = right * Mathf.Cos(angle) * r + forward * Mathf.Sin(angle) * r;
                Vector3 bottom = center + up * -halfHeight + radial;
                Vector3 top = center + up * halfHeight + radial;

                Gizmos.DrawLine(previousBottom, bottom);
                Gizmos.DrawLine(previousTop, top);

                if (i == 1 || i % 4 == 1)
                    Gizmos.DrawLine(bottom, top);

                previousBottom = bottom;
                previousTop = top;
            }
        }
    }
}
