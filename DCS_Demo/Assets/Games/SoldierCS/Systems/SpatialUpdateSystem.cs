using DCS.Actors;
using DCS.Core;
using DCS.Spatial;
using System.Collections.Generic;
using UnityEngine;

namespace DCS.Lua
{
    public sealed class SpatialUpdateSystem
    {
        private readonly DynamicSpatial _spatial;
        private readonly ComponentPool<PositionComponent> _pool;
        private readonly float _radius;

        private readonly Dictionary<ushort, Vector3> _lastPosition = new(1024);
        private readonly HashSet<ushort> _registered = new();

        public SpatialUpdateSystem(DynamicSpatial spatial, ComponentPool<PositionComponent> pool, float radius = 0.5f)
        {
            _spatial = spatial;
            _pool = pool;
            _radius = radius;
        }

        public void Update()
        {
            for (int i = 0; i < _pool.Partition; i++)
            {
                ref var pos = ref _pool.Components[i];
                int rosterIdx = pos.RosterIndex;
                Host host = _pool.Roster[rosterIdx].Host;
                if (!HostManager.IsValid(host)) continue;

                ushort hostId = host.Id;
                Vector3 p = pos.Position;
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
            }

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
    }
}