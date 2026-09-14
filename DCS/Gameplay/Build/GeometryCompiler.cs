#if UNITY_EDITOR
using DCS.Core;
using DCS.Interaction.Authoring;
using DCS.Spatial;
using System;
using UnityEngine;

namespace DCS.Gameplay.Build
{
    public static class GeometryCompiler
    {
        public static SpatialProxy CompileShape(BaseShape shape, ushort spatialId, ushort ownerId, ESpatialObjectType type, MapDataset dataset)
        {
            if (shape is ShapeBox box) return CompileBox(box, spatialId, ownerId, type, dataset);
            if (shape is ShapeSphere sphere) return CompileSphere(sphere, spatialId, ownerId, type, dataset);
            if (shape is ShapeCylinder cylinder) return CompileCylinder(cylinder, spatialId, ownerId, type, dataset);
            if (shape is ShapeBorder border) return CompileBorder(border, spatialId, ownerId, type, dataset);
            Debug.LogError($"[GeometryCompiler] Unsupported shape: {shape.GetType().Name}"); return default;
        }
        static SpatialProxy CompileBox(ShapeBox s, ushort sid, ushort oid, ESpatialObjectType type, MapDataset d)
        {
            Vector3 scale = Abs(s.transform.lossyScale); Vector3 ext = Vector3.Scale(s.Size, scale) * .5f; Vector3 c = s.transform.position; Quaternion r = s.transform.rotation;
            int i = d.Boxes.Count; d.Boxes.Add(new BoxGeometry { Center=c, Extents=ext, Rotation=r });
            return Proxy(sid, oid, type, new GeometryHandle(EGeometryType.Box, i), BoxBounds(c, ext, r));
        }
        static SpatialProxy CompileSphere(ShapeSphere s, ushort sid, ushort oid, ESpatialObjectType type, MapDataset d)
        {
            Vector3 sc = Abs(s.transform.lossyScale); float r = s.Radius * Mathf.Max(sc.x, Mathf.Max(sc.y, sc.z)); Vector3 c=s.transform.position; int i=d.Spheres.Count;
            d.Spheres.Add(new SphereGeometry { Center=c, Radius=r }); return Proxy(sid, oid, type, new GeometryHandle(EGeometryType.Sphere,i), new Bounds(c, Vector3.one*r*2f));
        }
        static SpatialProxy CompileCylinder(ShapeCylinder s, ushort sid, ushort oid, ESpatialObjectType type, MapDataset d)
        {
            Vector3 sc=Abs(s.transform.lossyScale); float r=s.Radius*Mathf.Max(sc.x,sc.z); float hh=s.Height*sc.y*.5f; Vector3 c=s.transform.position; Quaternion rot=s.transform.rotation; int i=d.Cylinders.Count;
            d.Cylinders.Add(new CylinderGeometry { Center=c, Radius=r, HalfHeight=hh, Rotation=rot }); return Proxy(sid,oid,type,new GeometryHandle(EGeometryType.Cylinder,i),CylinderBounds(c,r,hh,rot));
        }
        static SpatialProxy CompileBorder(ShapeBorder s, ushort sid, ushort oid, ESpatialObjectType type, MapDataset d)
        {
            int start=d.PolygonPoints.Count, count=s.PointCount; Vector3 min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity), max=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);
            for(int i=0;i<count;i++){Vector3 p=s.transform.TransformPoint(s.GetPoint(i)); d.PolygonPoints.Add(p); min=Vector3.Min(min,p); max=Vector3.Max(max,p);}
            if(count==0){min=max=s.transform.position;}
            int idx=d.Polygons.Count; d.Polygons.Add(new PolygonGeometry { StartIndex=start, PointCount=count, MinY=min.y, MaxY=max.y, Closed=s.Closed });
            return Proxy(sid,oid,type,new GeometryHandle(EGeometryType.Polygon,idx),new Bounds((min+max)*.5f,max-min));
        }
        static SpatialProxy Proxy(ushort sid,ushort oid,ESpatialObjectType t,GeometryHandle h,Bounds b)=>new SpatialProxy(sid,oid,t,h,b.min,b.max);
        static Vector3 Abs(Vector3 v)=>new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
        static Bounds BoxBounds(Vector3 c,Vector3 e,Quaternion r){Vector3 x=r*Vector3.right*e.x,y=r*Vector3.up*e.y,z=r*Vector3.forward*e.z;Vector3 w=new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z));return new Bounds(c,w*2f);}
        static Bounds CylinderBounds(Vector3 c,float r,float hh,Quaternion rot){Vector3 up=rot*Vector3.up,right=rot*Vector3.right,forward=rot*Vector3.forward,hx=right*r,hz=forward*r;Vector3 w=new Vector3(Mathf.Abs(hx.x)+Mathf.Abs(hz.x)+Mathf.Abs(up.x)*hh,Mathf.Abs(hx.y)+Mathf.Abs(hz.y)+Mathf.Abs(up.y)*hh,Mathf.Abs(hx.z)+Mathf.Abs(hz.z)+Mathf.Abs(up.z)*hh);return new Bounds(c,w*2f);}

        internal static bool TryCompile(BaseShape shape, ushort triggerId, ESpatialObjectType trigger, MapDataset dataset, out SpatialProxy proxy)
        {
            throw new NotImplementedException();
        }
    }
}
#endif
