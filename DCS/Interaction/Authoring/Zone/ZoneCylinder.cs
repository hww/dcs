using UnityEngine;

namespace DCS.Core
{
    public sealed class ZoneCylinder : BaseProxy
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

        public Vector3 Center => transform.position;
        public Quaternion Rotation => transform.rotation;

        private void OnDrawGizmos()
        {
            Gizmos.matrix = Matrix4x4.TRS(
                transform.position,
                transform.rotation,
                Vector3.one
            );

            DrawWireCylinder(
                Vector3.zero,
                _radius,
                _height
            );
        }

        private static void DrawWireCylinder(
            Vector3 center,
            float radius,
            float height)
        {
            const int segments = 32;

            float halfHeight = height * 0.5f;

            Vector3 bottomCenter =
                center + Vector3.down * halfHeight;

            Vector3 topCenter =
                center + Vector3.up * halfHeight;

            Vector3 previousBottom =
                bottomCenter + new Vector3(radius, 0f, 0f);

            Vector3 previousTop =
                topCenter + new Vector3(radius, 0f, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float angle =
                    i * Mathf.PI * 2f / segments;

                Vector3 offset =
                    new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius
                    );

                Vector3 currentBottom =
                    bottomCenter + offset;

                Vector3 currentTop =
                    topCenter + offset;

                Gizmos.DrawLine(
                    previousBottom,
                    currentBottom
                );

                Gizmos.DrawLine(
                    previousTop,
                    currentTop
                );

                // Вертикальные рёбра.
                if (i % 4 == 1)
                {
                    Gizmos.DrawLine(
                        currentBottom,
                        currentTop
                    );
                }

                previousBottom = currentBottom;
                previousTop = currentTop;
            }
        }
    }
}