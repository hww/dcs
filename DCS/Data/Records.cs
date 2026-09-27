using System;
using UnityEngine;

namespace DCS.Gameplay
{
    [Serializable] public struct EncounterRecord { public ushort Id; public string Key; public string ScriptPath; public bool Enabled; public bool AutoStart; public string FactsJson; }
    [Serializable] public struct RegionRecord { public ushort Id; public ushort EncounterId; public ushort StrongPointId; public string Key; public bool Enabled; public int ShapeCount; public string FactsJson; }
    [Serializable] public struct TriggerRecord { public ushort Id; public ushort EncounterId; public string Key; public bool Enabled; public byte Mode; public bool OneShot; public float Cooldown; public int ShapeCount; }
    [Serializable] public struct StrongPointRecord { public ushort Id; public ushort EncounterId; public ushort RegionId; public string Key; public Vector3 Position; public Quaternion Rotation; public bool Enabled; public int MinAgents; public int MaxAgents; public byte AllowedPostTypes; public int Priority; public string TagsJson; }
    [Serializable] public struct SpawnerRecord { public ushort HostId; public Vector3 Position; public string EntityClass; }
    [Serializable] public struct LocatorRecord { public ushort HostId; public Vector3 Position; public string TagsJson; }
    [Serializable] public struct NavigationSurfaceRecord { public ushort Id; public string Key; public bool Enabled; public byte SurfaceType; public int Cost; public ushort ShapeId; public string TagsJson; }
    [Serializable] public struct TraversalLinkRecord { public ushort Id; public string Key; public bool Enabled; public byte Type; public Vector3 Start; public Vector3 End; public string ActionKey; public string TagsJson; }
    [Serializable] public struct PatrolPathRecord { public ushort Id; public string Key; public bool Enabled; public bool Closed; public int StartPoint; public int PointCount; }
    [Serializable] public struct PostRecord { public ushort Id; public ushort StrongPointId; public Vector3 Position; public Quaternion Rotation; public byte Type; public bool Enabled; }
}
