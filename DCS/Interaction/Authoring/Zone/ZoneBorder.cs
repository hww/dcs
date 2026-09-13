using UnityEngine;

namespace DCS.Core
{
    public class ZoneBorder : BaseProxy
    {
        [Tooltip("Точки границы в локальном пространстве.")]
        public Vector3[] Points;

        [Tooltip("Замыкать последнюю точку с первой.")]
        public bool Closed = true;

        public int PointCount =>
            Points != null ? Points.Length : 0;

        public Vector3 GetPoint(int index)
        {
            if (Points == null || index < 0 || index >= Points.Length)
                return Vector3.zero;

            return Points[index];
        }

        public void SetPoint(int index, Vector3 localPosition)
        {
            if (Points == null || index < 0 || index >= Points.Length)
                return;
            Points[index] = localPosition;
        }

        private void OnDrawGizmos()
        {
            if (Points == null || Points.Length < 2)
                return;

            Gizmos.color = Color.white;

            for (int i = 0; i < Points.Length - 1; i++)
            {
                Gizmos.DrawLine(
                    transform.TransformPoint(Points[i]),
                    transform.TransformPoint(Points[i + 1]));
            }

            if (Closed && Points.Length >= 3)
            {
                Gizmos.DrawLine(
                    transform.TransformPoint(Points[Points.Length - 1]),
                    transform.TransformPoint(Points[0]));
            }
        }
    }
}

