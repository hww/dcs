using System;
using UnityEngine;

namespace DynamicComponent
{
    [Serializable]
    public struct ZoneRecord
    {
        public ushort HostId;
        public ushort Generation;
        public Vector3 Position;
        public float ActivationRadius;
        public string FactsJson;
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
