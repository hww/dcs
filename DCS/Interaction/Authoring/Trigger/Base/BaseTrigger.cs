using UnityEngine;
using DCS.Gameplay;
using DCS.Core;

namespace DCS.Interaction.Authoring
{
    public abstract class BaseTrigger : MonoBehaviour
    {
        public enum TriggerMode : byte { Enter = 0, Exit = 1, Stay = 2 }
        [SerializeField] private string _key;
        [SerializeField] private bool _enabled = true;
        [SerializeField] private TriggerMode _mode = TriggerMode.Enter;
        [SerializeField] private bool _oneShot;
        [SerializeField, Min(0f)] private float _cooldown;
        [SerializeField] private Encounter _targetEncounter;
        public string Key { get => _key; set => _key = value; }
        public bool Enabled { get => _enabled; set => _enabled = value; }
        public TriggerMode Mode { get => _mode; set => _mode = value; }
        public bool OneShot { get => _oneShot; set => _oneShot = value; }
        public float Cooldown { get => _cooldown; set => _cooldown = Mathf.Max(0f, value); }
        public Encounter TargetEncounter => _targetEncounter != null ? _targetEncounter : GetComponentInParent<Encounter>();
        public BaseShape[] GetShapes()
        {
            BaseShape[] found = GetComponentsInChildren<BaseShape>(true);
            int count = 0;
            for (int i = 0; i < found.Length; i++) if (found[i] != null && found[i].transform.parent == transform) count++;
            BaseShape[] result = new BaseShape[count];
            int index = 0;
            for (int i = 0; i < found.Length; i++) if (found[i] != null && found[i].transform.parent == transform) result[index++] = found[i];
            return result;
        }
    }
}
