using UnityEngine;

namespace DCS.Navigation.Authoring
{
    public enum TraversalLinkType : byte
    {
        Jump = 0,
        Drop = 1,
        Vault = 2,
        Climb = 3,
        GapCross = 4
    }

    [AddComponentMenu("DCS/Navigation/Traversal Link")]
    public sealed class TraversalLink : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private bool _enabled = true;
        [SerializeField] private TraversalLinkType _type;
        [SerializeField] private Transform _start;
        [SerializeField] private Transform _end;
        [SerializeField] private string _actionKey;
        [SerializeField] private string[] _tags;

        public string Key { get => _key; set => _key = value; }
        public bool Enabled { get => _enabled; set => _enabled = value; }
        public TraversalLinkType Type { get => _type; set => _type = value; }
        public Transform Start => _start != null ? _start : transform;
        public Transform End => _end;
        public string ActionKey { get => _actionKey; set => _actionKey = value; }
        public string[] Tags { get => _tags; set => _tags = value; }

        private void OnDrawGizmosSelected()
        {
            if (!Enabled || End == null) return;
            Gizmos.DrawLine(Start.position, End.position);
            Gizmos.DrawWireSphere(Start.position, 0.15f);
            Gizmos.DrawWireSphere(End.position, 0.15f);
        }
    }
}
