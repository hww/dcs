#if UNITY_EDITOR
using DCS.Interaction.Authoring;
using DCS.Spatial;
using System;
using UnityEngine;

namespace DCS.Gameplay.Build
{
    public static class GeometryCompiler
    {

        internal static bool TryCompile(
            BaseShape shape,
            ushort ownerId,
            ESpatialObjectType objectType,
            MapDataset dataset,
            out SpatialProxy proxy)
        {
            proxy = default;

            if (shape == null || dataset == null || !shape.Enabled)
                return false;

            ushort spatialId = (ushort)dataset.SpatialProxies.Count;

            switch (shape)
            {
                case ShapeBox box:
                    return CompileBox(
                        box,
                        spatialId,
                        ownerId,
                        objectType,
                        dataset,
                        out proxy);

                case ShapeSphere sphere:
                    return CompileSphere(
                        sphere,
                        spatialId,
                        ownerId,
                        objectType,
                        dataset,
                        out proxy);

                case ShapeCylinder cylinder:
                    return CompileCylinder(
                        cylinder,
                        spatialId,
                        ownerId,
                        objectType,
                        dataset,
                        out proxy);

                case ShapeBorder border:
                    return CompileBorder(
                        border,
                        spatialId,
                        ownerId,
                        objectType,
                        dataset,
                        out proxy);

                default:
                    Debug.LogError(
                        $"[GeometryCompiler] Unsupported Shape type: " +
                        $"{shape.GetType().FullName}",
                        shape);

                    return false;
            }
        }

        private static bool CompileBox(ShapeBox s,ushort sid,ushort oid,ESpatialObjectType type,MapDataset d,out SpatialProxy p){Vector3 sc=Abs(s.transform.lossyScale),e=Vector3.Scale(s.Size,sc)*.5f,c=s.transform.position;Quaternion r=s.transform.rotation;int i=d.Boxes.Count;d.Boxes.Add(new BoxGeometry{Center=c,Extents=e,Rotation=r});Bounds b=BoxBounds(c,e,r);p=new SpatialProxy(sid,oid,type,new GeometryHandle(EGeometryType.Box,i),b.min,b.max);return true;}
        private static bool CompileSphere(ShapeSphere s,ushort sid,ushort oid,ESpatialObjectType type,MapDataset d,out SpatialProxy p){Vector3 sc=Abs(s.transform.lossyScale);float r=s.Radius*Mathf.Max(sc.x,Mathf.Max(sc.y,sc.z));Vector3 c=s.transform.position;int i=d.Spheres.Count;d.Spheres.Add(new SphereGeometry{Center=c,Radius=r});Bounds b=new Bounds(c,Vector3.one*r*2f);p=new SpatialProxy(sid,oid,type,new GeometryHandle(EGeometryType.Sphere,i),b.min,b.max);return true;}
        private static bool CompileCylinder(ShapeCylinder s,ushort sid,ushort oid,ESpatialObjectType type,MapDataset d,out SpatialProxy p){Vector3 sc=Abs(s.transform.lossyScale);float r=s.Radius*Mathf.Max(sc.x,sc.z),hh=s.Height*sc.y*.5f;Vector3 c=s.transform.position;Quaternion rot=s.transform.rotation;int i=d.Cylinders.Count;d.Cylinders.Add(new CylinderGeometry{Center=c,Radius=r,HalfHeight=hh,Rotation=rot});Bounds b=CylinderBounds(c,r,hh,rot);p=new SpatialProxy(sid,oid,type,new GeometryHandle(EGeometryType.Cylinder,i),b.min,b.max);return true;}
        private static bool CompileBorder(ShapeBorder s,ushort sid,ushort oid,ESpatialObjectType type,MapDataset d,out SpatialProxy p){int count=s.PointCount;if(count<3){p=default;return false;}int start=d.PolygonPoints.Count;Vector3 min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity),max=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);for(int i=0;i<count;i++){Vector3 local=s.GetPoint(i);local.y=Mathf.Lerp(s.MinY,s.MaxY,0.5f);Vector3 world=s.transform.TransformPoint(local);d.PolygonPoints.Add(world);min=Vector3.Min(min,world);max=Vector3.Max(max,world);} // Y limits are authored independently and become runtime bounds.
            float minY=s.transform.TransformPoint(new Vector3(0f,s.MinY,0f)).y; float maxY=s.transform.TransformPoint(new Vector3(0f,s.MaxY,0f)).y; if(minY>maxY){float q=minY;minY=maxY;maxY=q;} min.y=minY; max.y=maxY; int iPoly=d.Polygons.Count;d.Polygons.Add(new PolygonGeometry{StartIndex=start,PointCount=count,MinY=minY,MaxY=maxY,Closed=s.Closed});p=new SpatialProxy(sid,oid,type,new GeometryHandle(EGeometryType.Polygon,iPoly),min,max);return true;}
        private static Vector3 Abs(Vector3 v)=>new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
        private static Bounds BoxBounds(Vector3 c,Vector3 e,Quaternion r){Vector3 x=r*Vector3.right*e.x,y=r*Vector3.up*e.y,z=r*Vector3.forward*e.z;Vector3 w=new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z));return new Bounds(c,w*2f);}
        private static Bounds CylinderBounds(Vector3 c,float r,float hh,Quaternion rot){Vector3 up=rot*Vector3.up,right=rot*Vector3.right,forward=rot*Vector3.forward;Vector3 x=right*r,z=forward*r,w=new Vector3(Mathf.Abs(x.x)+Mathf.Abs(z.x)+Mathf.Abs(up.x)*hh,Mathf.Abs(x.y)+Mathf.Abs(z.y)+Mathf.Abs(up.y)*hh,Mathf.Abs(x.z)+Mathf.Abs(z.z)+Mathf.Abs(up.z)*hh);return new Bounds(c,w*2f);}

   
    }
}
#endif
