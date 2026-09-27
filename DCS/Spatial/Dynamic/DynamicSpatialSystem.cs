using DCS.Core;
using DCS.Spatial;
using DCS.World;
using System.Collections.Generic;
using UnityEngine;

public sealed class DynamicSpatialSystem
{
    private readonly DynamicSpatial _spatial;
    private readonly NameIndex _names;
    private readonly TagIndex _tags;
    private readonly IPositionSource _positions;
    private readonly INameSource _nameSource;
    private readonly ITagSource _tagSource;
    private readonly float _radius;

    private readonly Dictionary<ushort, Vector3> _lastPosition = new(1024);
    private readonly HashSet<ushort> _registered = new();

    public DynamicSpatialSystem(
        DynamicSpatial spatial,
        NameIndex names,
        TagIndex tags,
        IPositionSource positions,
        INameSource nameSource,
        ITagSource tagSource,
        float radius = 0.5f)
    {
        _spatial = spatial;
        _names = names;
        _tags = tags;
        _positions = positions;
        _nameSource = nameSource;
        _tagSource = tagSource;
        _radius = radius;
    }

    public void Update()
    {
        _positions.ForEach(OnHostPosition);

        // Cleanup dead
        List<ushort> dead = null;
        foreach (var id in _registered)
        {
            Host h = new Host { Id = id, Generation = HostManager.GlobalHosts[id].Generation };
            if (!HostManager.IsValid(h))
            {
                dead ??= new List<ushort>();
                dead.Add(id);
            }
        }
        if (dead != null)
        {
            for (int i = 0; i < dead.Count; i++)
            {
                ushort id = dead[i];
                _spatial.Unregister(id);
                _registered.Remove(id);
                _lastPosition.Remove(id);
            }
            _spatial.Compact();
        }
    }

    private void OnHostPosition(Host host, Vector3 p)
    {
        ushort hostId = host.Id;
        bool isNew = !_registered.Contains(hostId);

        if (isNew)
        {
            Vector3 min = p - Vector3.one * _radius;
            Vector3 max = p + Vector3.one * _radius;
            _spatial.Register(hostId, ESpatialObjectType.Generic, min, max);
            _registered.Add(hostId);
            _lastPosition[hostId] = p;
        }
        else if (_lastPosition.TryGetValue(hostId, out var last) && last != p)
        {
            Vector3 min = p - Vector3.one * _radius;
            Vector3 max = p + Vector3.one * _radius;
            _spatial.Move(hostId, min, max);
            _lastPosition[hostId] = p;
        }

        if (_nameSource.TryGet(host, out string name) && !string.IsNullOrEmpty(name))
            _names.Add(name, hostId);

        if (_tagSource.TryGet(host, out uint mask) && mask != 0)
            _tags.Add(hostId, mask);
    }

    public void Clear()
    {
        _spatial.Clear();
        _registered.Clear();
        _lastPosition.Clear();
        _names.Clear();
        _tags.Clear();
    }
}