using UnityEngine;
using DCS.Spatial;

namespace DCS.Gameplay
{
    public sealed class TriggerRuntime
    {
        public ushort Id { get; }
        public ushort EncounterId { get; }
        public TriggerMode Mode { get; }
        public bool OneShot { get; }
        public float Cooldown { get; }
        public bool Enabled { get; private set; }
        public bool Fired { get; private set; }

        private bool _inside;
        private float _cooldownRemaining;

        public TriggerRuntime(TriggerRecord record)
        {
            Id = record.Id;
            EncounterId = record.EncounterId;
            Mode = (TriggerMode)record.Mode;
            OneShot = record.OneShot;
            Cooldown = record.Cooldown;
            Enabled = record.Enabled;
        }

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
        }

        public bool Evaluate(
            Vector3 point,
            SpatialRuntime spatial,
            float deltaTime)
        {
            if (!Enabled || Fired || spatial == null)
                return false;

            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining = Mathf.Max(
                    0f,
                    _cooldownRemaining - deltaTime);
            }

            bool inside = spatial.Contains(
                point,
                Id,
                ESpatialObjectType.Trigger);

            bool fire =
                Mode == TriggerMode.Enter
                    ? !_inside && inside
                    : Mode == TriggerMode.Exit
                        ? _inside && !inside
                        : inside;

            _inside = inside;

            if (!fire || _cooldownRemaining > 0f)
                return false;

            _cooldownRemaining = Cooldown;

            if (OneShot)
                Fired = true;

            return true;
        }
    }
}