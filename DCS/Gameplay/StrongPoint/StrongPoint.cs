using DCS.Interaction.Authoring;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Authored gameplay point of interest for systemic selection.
    /// It intentionally contains semantic data, not navigation or spatial-query logic.
    /// Regions may be authored as direct children when a StrongPoint needs an area of influence.
    /// </summary>
    [AddComponentMenu("DCS/Gameplay/Strong Point")]
    public sealed class StrongPoint : BaseProxy
    {
        [SerializeField]
        private string _key;

        [SerializeField]
        private int _priority;

        [SerializeField]
        private string[] _tags;

        [TextArea(2, 5)]
        [SerializeField]
        private string _description;

        [SerializeField]
        private int _minAgents = 0;

        [SerializeField]
        private int _maxAgents = 1;

        public string Key
        {
            get => _key;
            set => _key = value;
        }

        public int Priority
        {
            get => _priority;
            set => _priority = value;
        }

        public string[] Tags
        {
            get => _tags;
            set => _tags = value;
        }

        public int MaxAgents
        {
            get => _maxAgents;
            set => _maxAgents = value;
        }

        public int MinAgents
        {
            get => _minAgents;
            set => _minAgents = value;
        }

        public string Description
        {
            get => _description;
            set => _description = value;
        }

        public byte AllowedPostTypes;

        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
        public Vector3 Forward => transform.forward;

        public Region[] Regions => GetComponentsInChildren<Region>(true);

        private void OnDrawGizmosSelected()
        {
            Vector3 position = transform.position;
            Gizmos.DrawWireSphere(position, 0.35f);
            Gizmos.DrawLine(position, position + transform.forward * 1.5f);
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Key) ? name : Key;
        }
    }
}
