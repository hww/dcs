using UnityEngine;
using DCS.Gameplay;
using DCS.Interaction.Authoring;

namespace DCS.Gameplay
{
    [AddComponentMenu("DCS/Gameplay/Strong Point")]
    public sealed class StrongPoint : BaseProxy
    {
        [SerializeField] private string _key;
        [SerializeField] private int _priority;
        [SerializeField] private string[] _tags;
        [SerializeField] private bool _enabled = true;
        [SerializeField, Min(0)] private int _minAgents;
        [SerializeField, Min(0)] private int _maxAgents = 1;
        [SerializeField] private PostTypeMask _allowedPostTypes = PostTypeMask.Open | PostTypeMask.Cover;
        [TextArea(2, 5)] [SerializeField] private string _description;

        [System.Flags]
        public enum PostTypeMask : byte { None = 0, Open = 1, Cover = 2, Perch = 4, Climb = 8, All = 15 }

        public string Key { get => _key; set => _key = value; }
        public int Priority { get => _priority; set => _priority = value; }
        public string[] Tags { get => _tags; set => _tags = value; }
        public bool Enabled { get => _enabled; set => _enabled = value; }
        public int MinAgents { get => _minAgents; set => _minAgents = Mathf.Max(0, value); }
        public int MaxAgents { get => _maxAgents; set => _maxAgents = Mathf.Max(_minAgents, value); }
        public PostTypeMask AllowedPostTypes { get => _allowedPostTypes; set => _allowedPostTypes = value; }
        public string Description { get => _description; set => _description = value; }
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
        public Region[] Regions => GetComponentsInChildren<Region>(true);

        private void OnValidate() { if (_maxAgents < _minAgents) _maxAgents = _minAgents; }
        private void OnDrawGizmosSelected() { Gizmos.DrawWireSphere(transform.position, 0.35f); Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f); }
        public override string ToString() => string.IsNullOrEmpty(Key) ? name : Key;
    }
}
