using UnityEngine;

namespace DynamicComponent
{
    public class ZoneBorder : BaseProxy
    {
        [Tooltip("Точки, описывающие полигон или кривую линии границы зоны в локальном пространстве")]
        public Vector3[] Points;

        private void OnDrawGizmos()
        {
            if (Points == null || Points.Length < 2) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < Points.Length - 1; i++)
            {
                // Отрисовка гизмо в мировом пространстве для удобства дизайнера
                Gizmos.DrawLine(transform.TransformPoint(Points[i]), transform.TransformPoint(Points[i + 1]));
            }
        }
    }
}
