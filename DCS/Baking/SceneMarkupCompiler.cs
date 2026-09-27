#if UNITY_EDITOR
using DCS.Gameplay;
using DCS.Interaction.Authoring;
using DCS.Navigation.Authoring;
using DCS.Spatial;
using UnityEngine;

namespace DCS.Gameplay.Build
{
    public static class SceneMarkupCompiler
    {
        public static bool CompileRegion(Region region, MapDataset dataset, ushort regionId, ushort encounterId, ushort strongPointId, ref ushort spatialId)
        {
            if (region == null || dataset == null) return false;
            int shapeCount = 0;
            for (int i = 0; i < region.Shapes.Count; i++)
            {
                BaseShape shape = region.Shapes[i]; if (shape == null || !shape.Enabled) continue;
                if (GeometryCompiler.TryCompile(
                    shape,
                    regionId,
                    ESpatialObjectType.Region,
                    dataset,
                    out SpatialProxy proxy))
                {
                    dataset.SpatialProxies.Add(proxy);
                    shapeCount++;
                }
            }
            dataset.Regions.Add(new RegionRecord { Id=regionId, EncounterId=encounterId, StrongPointId=strongPointId, Key=region.Key??string.Empty, Enabled=region.Enabled, ShapeCount=shapeCount, FactsJson="{}" });
            return true;
        }

        public static bool CompileTrigger(Trigger trigger, MapDataset dataset, ushort triggerId, ushort encounterId, ref ushort spatialId)
        {
            if (trigger == null || dataset == null) return false;
            int shapeCount = 0;
            BaseShape[] shapes = trigger.GetShapes();
            for (int i = 0; i < shapes.Length; i++)
            {
                if (GeometryCompiler.TryCompile(
                    shapes[i],
                    triggerId,
                    ESpatialObjectType.Trigger,
                    dataset,
                    out SpatialProxy proxy))
                {
                    dataset.SpatialProxies.Add(proxy);
                    shapeCount++;
                }
            }
            dataset.Triggers.Add(new TriggerRecord { Id=triggerId, EncounterId=encounterId, Key=trigger.Key??string.Empty, Enabled=trigger.Enabled, Mode=(byte)trigger.Mode, OneShot=trigger.OneShot, Cooldown=trigger.Cooldown, ShapeCount=shapeCount });
            return true;
        }

        public static bool CompileStrongPoint(StrongPoint sp, MapDataset dataset, ushort id, ushort encounterId)
        {
            if (sp == null || dataset == null) return false;
            string tags = "{}";
            if (sp.Tags != null && sp.Tags.Length > 0) tags = JsonUtility.ToJson(new TagsWrapper { Values=sp.Tags });
            dataset.StrongPoints.Add(new StrongPointRecord { Id=id, EncounterId=encounterId, RegionId=0, Key=sp.Key??string.Empty, Position=sp.Position, Rotation=sp.Rotation, Enabled=sp.Enabled, MinAgents=sp.MinAgents, MaxAgents=sp.MaxAgents, AllowedPostTypes=(byte)sp.AllowedPostTypes, Priority=sp.Priority, TagsJson=tags });
            return true;
        }

        public static bool CompileNavigationSurface(NavigationSurface surface, MapDataset dataset, ushort id, ref ushort spatialId)
        {
            if (surface == null || dataset == null || !surface.Enabled || surface.Shape == null) return false;
            if (!GeometryCompiler.TryCompile(surface.Shape,
                                             id,
                                             ESpatialObjectType.NavigationSurface,
                                             dataset,
                                             out SpatialProxy proxy))
            {
                return false;
            }
            dataset.SpatialProxies.Add(proxy); spatialId++;
            string tags = "{}"; if (surface.Tags != null && surface.Tags.Length > 0) tags=JsonUtility.ToJson(new TagsWrapper{Values=surface.Tags});
            dataset.NavigationSurfaces.Add(new NavigationSurfaceRecord{Id=id,Key=surface.Key??string.Empty,Enabled=surface.Enabled,SurfaceType=(byte)surface.SurfaceType,Cost=surface.Cost,ShapeId=proxy.SpatialId,TagsJson=tags});
            return true;
        }

        public static void CompileTraversalLink(TraversalLink link, MapDataset dataset, ushort id)
        {
            if (link == null || dataset == null || !link.Enabled || link.End == null) return;
            string tags="{}"; if(link.Tags!=null&&link.Tags.Length>0)tags=JsonUtility.ToJson(new TagsWrapper{Values=link.Tags});
            dataset.TraversalLinks.Add(new TraversalLinkRecord{Id=id,Key=link.Key??string.Empty,Enabled=link.Enabled,Type=(byte)link.Type,Start=link.Start.position,End=link.End.position,ActionKey=link.ActionKey??string.Empty,TagsJson=tags});
        }

        public static void CompilePatrolPath(PatrolPath path, MapDataset dataset, ushort id)
        {
            if (path == null || dataset == null || !path.Enabled || path.PointCount < 2) return;
            int start=dataset.PatrolPoints.Count; for(int i=0;i<path.PointCount;i++)dataset.PatrolPoints.Add(path.GetWorldPoint(i));
            dataset.PatrolPaths.Add(new PatrolPathRecord{Id=id,Key=path.Key??string.Empty,Enabled=path.Enabled,Closed=path.Closed,StartPoint=start,PointCount=path.PointCount});
        }

        [System.Serializable] private struct TagsWrapper { public string[] Values; }
    }
}
#endif
