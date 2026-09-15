using System;
using System.Collections.Generic;
using UnityEngine;

namespace DCS.Spatial
{
    public sealed class SpatialIndex
    {
        private readonly float _cellSize;
        private readonly Dictionary<Vector3Int, List<int>> _grid = new Dictionary<Vector3Int, List<int>>();
        private SpatialProxy[] _proxies = Array.Empty<SpatialProxy>();
        private RuntimeGeometryStorage _geometry;

        public SpatialIndex(float cellSize = 15f) { _cellSize = Mathf.Max(0.1f, cellSize); }
        public int ProxyCount => _proxies.Length;

        public void Load(IReadOnlyList<SpatialProxy> proxies, RuntimeGeometryStorage geometry)
        {
            Clear();
            if (proxies == null || geometry == null) return;
            _geometry = geometry; _proxies = new SpatialProxy[proxies.Count];
            for (int i = 0; i < proxies.Count; i++) { _proxies[i] = proxies[i]; RegisterProxy(i, _proxies[i]); }
        }

        public void Clear() { _grid.Clear(); _proxies = Array.Empty<SpatialProxy>(); _geometry = null; }
        public bool TryGetProxy(int index, out SpatialProxy proxy) { if ((uint)index >= (uint)_proxies.Length) { proxy = default; return false; } proxy = _proxies[index]; return true; }

        public void QueryPoint(Vector3 point, SpatialQueryFilter filter, List<ushort> results)
        {
            if (results == null) return; results.Clear();
            if (_geometry == null) return;
            Vector3Int cell = WorldToCell(point);
            if (!_grid.TryGetValue(cell, out List<int> candidates)) return;
            for (int i = 0; i < candidates.Count; i++)
            {
                SpatialProxy proxy = _proxies[candidates[i]];
                if (!Matches(proxy, filter) || !BoundsContainsPoint(proxy, point)) continue;
                if (GeometryIntersection.IsPointInside(point, proxy.Geometry, _geometry)) AddUnique(results, proxy.OwnerId);
            }
        }

        public void QueryRadius(Vector3 center, float radius, SpatialQueryFilter filter, List<ushort> results)
        {
            if (results == null) return; results.Clear(); if (_geometry == null || radius < 0f) return;
            Bounds b = new Bounds(center, Vector3.one * radius * 2f);
            Vector3Int min = WorldToCell(b.min), max = WorldToCell(b.max);
            for (int x = min.x; x <= max.x; x++) for (int y = min.y; y <= max.y; y++) for (int z = min.z; z <= max.z; z++)
            {
                if (!_grid.TryGetValue(new Vector3Int(x, y, z), out List<int> candidates)) continue;
                for (int i = 0; i < candidates.Count; i++) { SpatialProxy proxy = _proxies[candidates[i]]; if (Matches(proxy, filter) && BoundsIntersectsSphere(proxy, center, radius)) AddUnique(results, proxy.OwnerId); }
            }
        }

        public bool Contains(Vector3 point, SpatialQueryFilter filter)
        {
            if (_geometry == null) return false;
            Vector3Int cell = WorldToCell(point);
            if (!_grid.TryGetValue(cell, out List<int> candidates)) return false;
            for (int i = 0; i < candidates.Count; i++)
            {
                SpatialProxy proxy = _proxies[candidates[i]];
                if (!Matches(proxy, filter) || !BoundsContainsPoint(proxy, point)) continue;
                if (GeometryIntersection.IsPointInside(point, proxy.Geometry, _geometry)) return true;
            }
            return false;
        }

        public bool QueryNearest(Vector3 point, float maxDistance, SpatialQueryFilter filter, out SpatialHit hit)
        {
            hit = default; if (_geometry == null || maxDistance < 0f) return false;
            float best = maxDistance;
            bool found = false;
            for (int i = 0; i < _proxies.Length; i++)
            {
                SpatialProxy proxy = _proxies[i]; if (!Matches(proxy, filter)) continue;
                float d = DistanceToBounds(point, proxy); if (d > best) continue;
                if (GeometryIntersection.IsPointInside(point, proxy.Geometry, _geometry)) d = 0f;
                if (d < best) { best = d; found = true; hit = new SpatialHit { SpatialId=proxy.SpatialId, OwnerId=proxy.OwnerId, ObjectType=proxy.ObjectType, GeometryType=proxy.Geometry.Type, Position=point, Distance=d, Normal=Vector3.zero }; }
            }
            return found;
        }

        public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, SpatialQueryFilter filter, out SpatialHit hit)
        {
            hit = default; if (_geometry == null || maxDistance < 0f || direction.sqrMagnitude < 1e-8f) return false;
            direction.Normalize(); float best = maxDistance; bool found = false;
            for (int i = 0; i < _proxies.Length; i++)
            {
                SpatialProxy proxy = _proxies[i]; if (!Matches(proxy, filter) || !RayIntersectsBounds(origin, direction, best, proxy)) continue;
                if (!GeometryIntersection.Raycast(origin, direction, best, proxy.Geometry, _geometry, out Vector3 p, out Vector3 n, out float d)) continue;
                if (d < best) { best=d; found=true; hit=new SpatialHit{SpatialId=proxy.SpatialId,OwnerId=proxy.OwnerId,ObjectType=proxy.ObjectType,GeometryType=proxy.Geometry.Type,Position=p,Normal=n,Distance=d}; }
            }
            return found;
        }

        private bool Matches(SpatialProxy p, SpatialQueryFilter f) => (!f.FilterByType || p.ObjectType == f.ObjectType) && (!f.FilterByOwner || p.OwnerId == f.OwnerId);
        private void RegisterProxy(int index, SpatialProxy p) { Vector3Int min=WorldToCell(p.AABBMin),max=WorldToCell(p.AABBMax); for(int x=min.x;x<=max.x;x++)for(int y=min.y;y<=max.y;y++)for(int z=min.z;z<=max.z;z++){Vector3Int c=new Vector3Int(x,y,z);if(!_grid.TryGetValue(c,out var list)){list=new List<int>();_grid.Add(c,list);}list.Add(index);} }
        private Vector3Int WorldToCell(Vector3 p) => new Vector3Int(Mathf.FloorToInt(p.x/_cellSize),Mathf.FloorToInt(p.y/_cellSize),Mathf.FloorToInt(p.z/_cellSize));
        private static bool BoundsContainsPoint(SpatialProxy p,Vector3 v)=>v.x>=p.AABBMin.x&&v.x<=p.AABBMax.x&&v.y>=p.AABBMin.y&&v.y<=p.AABBMax.y&&v.z>=p.AABBMin.z&&v.z<=p.AABBMax.z;
        private static bool BoundsIntersectsSphere(SpatialProxy p,Vector3 c,float r){float x=Mathf.Clamp(c.x,p.AABBMin.x,p.AABBMax.x),y=Mathf.Clamp(c.y,p.AABBMin.y,p.AABBMax.y),z=Mathf.Clamp(c.z,p.AABBMin.z,p.AABBMax.z);float dx=c.x-x,dy=c.y-y,dz=c.z-z;return dx*dx+dy*dy+dz*dz<=r*r;}
        private static float DistanceToBounds(Vector3 p,SpatialProxy s){float x=Mathf.Max(s.AABBMin.x-Mathf.Max(p.x,s.AABBMin.x),0f);x=Mathf.Max(x,Mathf.Max(p.x-s.AABBMax.x,0f));float y=Mathf.Max(s.AABBMin.y-Mathf.Max(p.y,s.AABBMin.y),0f);y=Mathf.Max(y,Mathf.Max(p.y-s.AABBMax.y,0f));float z=Mathf.Max(s.AABBMin.z-Mathf.Max(p.z,s.AABBMin.z),0f);z=Mathf.Max(z,Mathf.Max(p.z-s.AABBMax.z,0f));return Mathf.Sqrt(x*x+y*y+z*z);}
        private static bool RayIntersectsBounds(Vector3 o,Vector3 d,float max,SpatialProxy p){float tmin=0f,tmax=max; if(!RaySlab(o.x,d.x,p.AABBMin.x,p.AABBMax.x,ref tmin,ref tmax)||!RaySlab(o.y,d.y,p.AABBMin.y,p.AABBMax.y,ref tmin,ref tmax)||!RaySlab(o.z,d.z,p.AABBMin.z,p.AABBMax.z,ref tmin,ref tmax))return false;return true;}
        private static bool RaySlab(float o,float d,float min,float max,ref float tmin,ref float tmax){if(Mathf.Abs(d)<1e-7f)return o>=min&&o<=max;float a=(min-o)/d,b=(max-o)/d;if(a>b){float q=a;a=b;b=q;}tmin=Mathf.Max(tmin,a);tmax=Mathf.Min(tmax,b);return tmin<=tmax&&tmax>=0f;}
        private static void AddUnique(List<ushort> list,ushort id){for(int i=0;i<list.Count;i++)if(list[i]==id)return;list.Add(id);}
    }
}
