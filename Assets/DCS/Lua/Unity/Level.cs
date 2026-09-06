using System;

namespace DynamicComponent
{
    public enum ELevelStatus
    {
        Inactive,
        Active,
        Loading,
        Loaded,
        Unload,
        Unloading,
        Error
    }

    /// <summary>
    /// Level — a passive structural container mapping configuration parameters of master world scenes.
    /// Pure Data-Driven resource asset managed entirely by the high-level system framework loop.
    /// </summary>
    [System.Serializable]
    public class Level
    {
        public string Name;
        public ELevelStatus Status;
        public DateTime LoadTime;
        public int Priority;

        public void SetOrClearStatus(ELevelStatus status, bool value)
        {
            if (value)
                SetStatus(status);
            else
                Status = ELevelStatus.Inactive; // Fallback to inactive layout reference
        }

        public bool CheckStatus(ELevelStatus status)
        {
            return Status == status;
        }

        public void SetStatus(ELevelStatus status)
        {
            Status = status;
            if (status == ELevelStatus.Loaded)
                LoadTime = DateTime.Now;
        }

        public TimeSpan GetTimeSinceLoad()
        {
            return DateTime.Now - LoadTime;
        }

        public bool CanBeUnloaded()
        {
            return Status == ELevelStatus.Loaded || Status == ELevelStatus.Error;
        }

        public bool CanBeLoaded()
        {
            return Status == ELevelStatus.Inactive || Status == ELevelStatus.Error;
        }

        public override string ToString()
        {
            return $"{Name} (Status: {Status}, Loaded: {LoadTime:HH:mm:ss})";
        }
    }
}
