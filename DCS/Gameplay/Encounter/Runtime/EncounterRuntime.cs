using System;

namespace DCS.Gameplay
{
    public enum EncounterState : byte
    {
        Inactive = 0,
        Active = 1,
        Completed = 2,
        Failed = 3
    }

    public sealed class EncounterRuntime
    {
        public ushort Id { get; }
        public string Key { get; }
        public string ScriptPath { get; }
        public bool Enabled { get; private set; }
        public EncounterState State { get; private set; }

        public event Action<EncounterRuntime, EncounterState, EncounterState> StateChanged;

        public EncounterRuntime(EncounterRecord record)
        {
            Id = record.Id;
            Key = record.Key;
            ScriptPath = record.ScriptPath;
            Enabled = true;
            State = EncounterState.Inactive;
        }

        public bool Activate()
        {
            if (!Enabled || State == EncounterState.Completed || State == EncounterState.Failed)
                return false;

            SetState(EncounterState.Active);
            return true;
        }

        public bool Complete() => TransitionFromActive(EncounterState.Completed);
        public bool Fail() => TransitionFromActive(EncounterState.Failed);

        public bool Deactivate()
        {
            if (State != EncounterState.Active)
                return false;

            SetState(EncounterState.Inactive);
            return true;
        }

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            if (!enabled && State == EncounterState.Active)
                Deactivate();
        }

        private bool TransitionFromActive(EncounterState target)
        {
            if (State != EncounterState.Active)
                return false;

            SetState(target);
            return true;
        }

        private void SetState(EncounterState value)
        {
            if (State == value)
                return;

            EncounterState previous = State;
            State = value;
            StateChanged?.Invoke(this, previous, value);
        }
    }
}
