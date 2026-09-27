using UnityEngine;

namespace DCS.Interaction.Authoring
{
    [AddComponentMenu("DCS/Spatial/Shape Border")]
    public sealed class ShapeBorder : BaseShape
    {
        [SerializeField]
        private Vector3[] _points =
        {
            new Vector3(-1f, 0f, -1f),
            new Vector3(-1f, 0f, 1f),
            new Vector3(1f, 0f, 1f),
            new Vector3(1f, 0f, -1f)
        };

        [SerializeField]
        private bool _closed = true;

        [SerializeField]
        private float _minY = 0f;

        [SerializeField]
        private float _maxY = 2f;

        public bool Closed
        {
            get => _closed;
            set => _closed = value;
        }

        public float MinY
        {
            get => _minY;
            set => _minY = value;
        }

        public float MaxY
        {
            get => _maxY;
            set => _maxY = value;
        }

        public int PointCount => _points?.Length ?? 0;

        public Vector3[] Points => _points;

        public Vector3 GetPoint(int index) => _points[index];

        public void SetPoint(int index, Vector3 point)
        {
            _points[index] = point;
        }

        public void AddPoint(Vector3 point)
        {
            int oldLength = _points?.Length ?? 0;
            System.Array.Resize(ref _points, oldLength + 1);
            _points[oldLength] = point;
        }

        public void InsertPoint(int index, Vector3 point)
        {
            if (_points == null)
                _points = System.Array.Empty<Vector3>();

            index = Mathf.Clamp(index, 0, _points.Length);
            Vector3[] result = new Vector3[_points.Length + 1];

            for (int i = 0, j = 0; i < result.Length; i++)
            {
                if (i == index)
                {
                    result[i] = point;
                    continue;
                }

                result[i] = _points[j++];
            }

            _points = result;
        }

        public void RemovePoint(int index)
        {
            if (_points == null || index < 0 || index >= _points.Length)
                return;

            Vector3[] result = new Vector3[_points.Length - 1];

            for (int i = 0, j = 0; i < _points.Length; i++)
            {
                if (i == index)
                    continue;

                result[j++] = _points[i];
            }

            _points = result;
        }

        private void OnValidate()
        {
            if (_maxY < _minY)
                _maxY = _minY;
        }

        protected override void DrawShapeGizmo()
        {
            if (_points == null || _points.Length < 2)
                return;

            DrawLoop(_minY);
            DrawLoop(_maxY);

            if (_closed && _points.Length >= 3)
            {
                Gizmos.DrawLine(
                    transform.TransformPoint(new Vector3(_points[0].x, _minY, _points[0].z)),
                    transform.TransformPoint(new Vector3(_points[0].x, _maxY, _points[0].z)));

                for (int i = 1; i < _points.Length; i++)
                {
                    if (i % 2 == 0)
                        continue;

                    Gizmos.DrawLine(
                        transform.TransformPoint(new Vector3(_points[i].x, _minY, _points[i].z)),
                        transform.TransformPoint(new Vector3(_points[i].x, _maxY, _points[i].z)));
                }
            }
        }

        private void DrawLoop(float y)
        {
            for (int i = 0; i < _points.Length - 1; i++)
            {
                Vector3 a = new Vector3(_points[i].x, y, _points[i].z);
                Vector3 b = new Vector3(_points[i + 1].x, y, _points[i + 1].z);
                Gizmos.DrawLine(transform.TransformPoint(a), transform.TransformPoint(b));
            }

            if (_closed && _points.Length > 2)
            {
                Vector3 a = new Vector3(_points[_points.Length - 1].x, y, _points[_points.Length - 1].z);
                Vector3 b = new Vector3(_points[0].x, y, _points[0].z);
                Gizmos.DrawLine(transform.TransformPoint(a), transform.TransformPoint(b));
            }
        }
    }
}
