#if UNITY_EDITOR
using System.Collections.Generic;
using DCS.Core;
using DCS.Interaction.Authoring;
using DCS.Spatial;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DCS.Gameplay.Build
{
    public static class WorldPacker
    {
        [MenuItem("DCS Engine/Bake Active Map Assets")]
        public static void BakeActiveSceneMenu()
        {
            Scene scene=SceneManager.GetActiveScene(); if(!scene.IsValid()||!scene.isLoaded){Debug.LogError("[WorldPacker] Active scene invalid.");return;}
            const string folder="Assets/Resources/Maps"; EnsureFolder(folder); string path=$"{folder}/{scene.name}_dataset.asset";
            MapDataset d=AssetDatabase.LoadAssetAtPath<MapDataset>(path); if(d==null){d=ScriptableObject.CreateInstance<MapDataset>();AssetDatabase.CreateAsset(d,path);}
            d.Clear(); d.MapName=scene.name; HashSet<ushort> used=new HashSet<ushort>();
            foreach(GameObject root in scene.GetRootGameObjects()){BakeEncounters(root,d,used); BakeActors(root,d);}
            EditorUtility.SetDirty(d); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log($"[WorldPacker] Baked {scene.name}: encounters={d.Encounters.Count}, regions={d.Regions.Count}, triggers={d.Triggers.Count}, strongPoints={d.StrongPoints.Count}, proxies={d.SpatialProxies.Count}");
        }
        static void BakeEncounters(GameObject root,MapDataset d,HashSet<ushort> used)
        {
            Encounter[] es=root.GetComponentsInChildren<Encounter>(true); for(int i=0;i<es.Length;i++){Encounter e=es[i]; if(e==null)continue; ushort eid=BuildIdUtility.GetId(e,used); d.Encounters.Add(new EncounterRecord{Id=eid,Key=e.Key??string.Empty,ScriptPath=e.ScriptPath??string.Empty});
                StrongPoint[] sps=e.GetComponentsInChildren<StrongPoint>(true); for(int s=0;s<sps.Length;s++)if(sps[s].transform.parent==e.transform)BakeStrongPoint(sps[s],eid,d,used);
                Region[] rs=e.GetComponentsInChildren<Region>(true); for(int r=0;r<rs.Length;r++)if(rs[r].transform.parent==e.transform)BakeRegion(rs[r],eid,0,d,used);
                Trigger[] ts=e.GetComponentsInChildren<Trigger>(true); for(int t=0;t<ts.Length;t++)if(ts[t].transform.parent==e.transform)BakeTrigger(ts[t],eid,d,used);
            }
        }
        static void BakeStrongPoint(StrongPoint sp,ushort eid,MapDataset d,HashSet<ushort> used)
        {
            ushort id=BuildIdUtility.GetId(sp,used); StrongPointRecord rec=new StrongPointRecord{
                Id=id,
                EncounterId=eid,
                RegionId=0,
                Key=sp.Key??string.Empty,
                Enabled=sp.Enabled,
                MinAgents=sp.MinAgents,
                MaxAgents=sp.MaxAgents,
                AllowedPostTypes=(byte)sp.AllowedPostTypes}; 
            d.StrongPoints.Add(rec);
            Region[] rs=sp.Regions; for(int i=0;i<rs.Length;i++)if(rs[i].transform.parent==sp.transform){ushort rid=BakeRegion(rs[i],eid,id,d,used);rec=d.StrongPoints[d.StrongPoints.Count-1];rec.RegionId=rid;d.StrongPoints[d.StrongPoints.Count-1]=rec;break;}
        }
        static ushort BakeRegion(Region r,ushort eid,ushort spid,MapDataset d,HashSet<ushort> used)
        {
            ushort id=BuildIdUtility.GetId(r,used); d.Regions.Add(new RegionRecord{Id=id,EncounterId=eid,StrongPointId=spid,Key=r.Key??string.Empty,Enabled=r.Enabled});
            IReadOnlyList<BaseShape> shapes=r.Shapes; for(int i=0;i<shapes.Count;i++){BaseShape s=shapes[i];if(s==null||!s.Enabled)continue;ushort sid=BuildIdUtility.GetId(s,used);d.SpatialProxies.Add(GeometryCompiler.CompileShape(s,sid,id,ESpatialObjectType.Region,d));} return id;
        }
        static void BakeTrigger(Trigger t,ushort eid,MapDataset d,HashSet<ushort> used)
        {
            ushort id=BuildIdUtility.GetId(t,used);d.Triggers.Add(new TriggerRecord{Id=id,EncounterId=eid,Key=t.Key??string.Empty,Enabled=t.Enabled,Mode=(byte)t.Mode,OneShot=t.OneShot,Cooldown=t.Cooldown});
            BaseShape[] ss=t.GetShapes();for(int i=0;i<ss.Length;i++){BaseShape s=ss[i];if(s==null||!s.Enabled)continue;ushort sid=BuildIdUtility.GetId(s,used);d.SpatialProxies.Add(GeometryCompiler.CompileShape(s,sid,id,ESpatialObjectType.Trigger,d));}
        }
        static void BakeActors(GameObject root,MapDataset d)
        {
            Spawner[] ss=root.GetComponentsInChildren<Spawner>(true);for(int i=0;i<ss.Length;i++)d.Spawners.Add(new SpawnerRecord{HostId=ss[i].Host.Id,Position=ss[i].transform.position,EntityClass=ss[i].entityClassToSpawn??string.Empty});
            Locator[] ls=root.GetComponentsInChildren<Locator>(true);for(int i=0;i<ls.Length;i++)if(!(ls[i] is Spawner))d.Locators.Add(new LocatorRecord{HostId=ls[i].Host.Id,Position=ls[i].transform.position,TagsJson=ls[i].Facts!=null?ls[i].Facts.ToJson():"{}"});
        }
        static void EnsureFolder(string folder){if(AssetDatabase.IsValidFolder(folder))return;string[] p=folder.Split('/');string c=p[0];for(int i=1;i<p.Length;i++){string n=c+"/"+p[i];if(!AssetDatabase.IsValidFolder(n))AssetDatabase.CreateFolder(c,p[i]);c=n;}}
    }
}
#endif
