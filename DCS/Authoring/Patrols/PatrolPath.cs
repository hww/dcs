using UnityEngine;

namespace DCS.Gameplay
{
    /// <summary>Authored patrol/search loop. Runtime may sample or compile it.</summary>
    [AddComponentMenu("DCS/Gameplay/Patrol Path")]
    public sealed class PatrolPath : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private bool _enabled = true;
        [SerializeField] private bool _closed = true;
        [SerializeField] private Vector3[] _points =
        {
            new Vector3(-2f, 0f, 0f),
            new Vector3(0f, 0f, 2f),
            new Vector3(2f, 0f, 0f)
        };

        public string Key { get => _key; set => _key = value; }
        public bool Enabled { get => _enabled; set => _enabled = value; }
        public bool Closed { get => _closed; set => _closed = value; }
        public int PointCount => _points?.Length ?? 0;
        public Vector3 GetPoint(int index) => _points[index];
        public Vector3 GetWorldPoint(int index) => transform.TransformPoint(_points[index]);

        private void OnValidate() { _points ??= System.Array.Empty<Vector3>(); }

        private void OnDrawGizmosSelected()
        {
            if (!_enabled || _points == null || _points.Length < 2) return;
            for (int i = 0; i < _points.Length - 1; i++) Gizmos.DrawLine(GetWorldPoint(i), GetWorldPoint(i + 1));
            if (_closed && _points.Length > 2) Gizmos.DrawLine(GetWorldPoint(_points.Length - 1), GetWorldPoint(0));
        }
    }
}
