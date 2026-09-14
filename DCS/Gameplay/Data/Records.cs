using System;
using UnityEngine;
using DCS.Spatial;

namespace DCS.Gameplay
{
    [Serializable]
    public struct EncounterRecord
    {
        public ushort Id;
        public string Key;
        public string ScriptPath;
        public bool AutoStart;
        public string FactsJson;
    }

    [Serializable]
    public struct RegionRecord
    {
        public ushort Id;
        public ushort EncounterId;
        public ushort StrongPointId;
        public string Key;
        public bool Enabled;
        public ushort ShapeCount;
        public string FactsJson;
    }

    [Serializable]
    public struct TriggerRecord
    {
        public ushort Id;
        public ushort EncounterId;
        public string Key;
        public bool Enabled;
        public byte Mode;
        public bool OneShot;
        public float Cooldown;
        public ushort ShapeCount;
    }

    [Serializable]
    public struct StrongPointRecord
    {
        public ushort Id;
        public ushort EncounterId;
        public ushort RegionId;

        public string Key;

        public Vector3 Position;
        public Quaternion Rotation;

        public bool Enabled;

        public int MinAgents;
        public int MaxAgents;

        public byte AllowedPostTypes;

        public int Priority;

        public string TagsJson;
    }

    [Serializable]
    public struct SpawnerRecord
    {
        public ushort HostId;
        public Vector3 Position;
        public string EntityClass;
    }

    [Serializable]
    public struct LocatorRecord
    {
        public ushort HostId;
        public Vector3 Position;
        public string TagsJson;
    }
}
