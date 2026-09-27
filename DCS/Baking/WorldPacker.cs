#if UNITY_EDITOR
using System.Collections.Generic;
using DCS.Core;
using DCS.Interaction.Authoring;
using DCS.Navigation.Authoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DCS.Gameplay.Build
{
    public static class WorldPacker
    {
        [MenuItem("DCS/World/Bake Active Scene")]
        public static void BakeActiveSceneMenu()
        {
            Scene scene=SceneManager.GetActiveScene();
            if(!scene.IsValid()||!scene.isLoaded){Debug.LogError("[WorldPacker] Active scene is invalid or unloaded.");return;}
            const string folder="Assets/Resources/Maps"; EnsureFolder(folder);
            string path=$"{folder}/{scene.name}_dataset.asset";
            MapDataset dataset=AssetDatabase.LoadAssetAtPath<MapDataset>(path);
            if(dataset==null){dataset=ScriptableObject.CreateInstance<MapDataset>();AssetDatabase.CreateAsset(dataset,path);}
            dataset.Clear();dataset.MapName=scene.name;
            HashSet<ushort> used=new HashSet<ushort>(); ushort spatialId=1;

            foreach(GameObject root in scene.GetRootGameObjects())
            {
                Encounter[] encounters=root.GetComponentsInChildren<Encounter>(true);
                for(int i=0;i<encounters.Length;i++) BakeEncounter(encounters[i],dataset,used,ref spatialId);
                BakeNavigation(root,dataset,used,ref spatialId);
                BakeActors(root,dataset);
            }
            EditorUtility.SetDirty(dataset);AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log($"[WorldPacker] {scene.name}: encounters={dataset.Encounters.Count}, regions={dataset.Regions.Count}, triggers={dataset.Triggers.Count}, strongPoints={dataset.StrongPoints.Count}, surfaces={dataset.NavigationSurfaces.Count}, traversalLinks={dataset.TraversalLinks.Count}, patrolPaths={dataset.PatrolPaths.Count}, proxies={dataset.SpatialProxies.Count}");
        }

        private static void BakeEncounter(Encounter e,MapDataset d,HashSet<ushort> used,ref ushort spatialId)
        {
            if(e==null)return; if(!TryId(e,used,out ushort eid))return;
            d.Encounters.Add(new EncounterRecord{Id=eid,Key=e.Key??string.Empty,ScriptPath=e.ScriptPath??string.Empty,Enabled=e.Enabled,AutoStart=e.AutoStart,FactsJson="{}"});
            StrongPoint[] sps=e.GetComponentsInChildren<StrongPoint>(true);
            for(int i=0;i<sps.Length;i++)if(sps[i]!=null&&sps[i].transform.parent==e.transform){if(!TryId(sps[i],used,out ushort sid))continue;SceneMarkupCompiler.CompileStrongPoint(sps[i],d,sid,eid);Region[] rs=sps[i].Regions;for(int r=0;r<rs.Length;r++)if(rs[r]!=null&&rs[r].transform.parent==sps[i].transform){if(TryId(rs[r],used,out ushort rid))SceneMarkupCompiler.CompileRegion(rs[r],d,rid,eid,sid,ref spatialId);}}
            Region[] regions=e.GetComponentsInChildren<Region>(true);for(int i=0;i<regions.Length;i++)if(regions[i]!=null&&regions[i].transform.parent==e.transform){if(TryId(regions[i],used,out ushort rid))SceneMarkupCompiler.CompileRegion(regions[i],d,rid,eid,0,ref spatialId);}
            Trigger[] ts=e.GetComponentsInChildren<Trigger>(true);for(int i=0;i<ts.Length;i++)if(ts[i]!=null&&ts[i].transform.parent==e.transform){if(TryId(ts[i],used,out ushort tid))SceneMarkupCompiler.CompileTrigger(ts[i],d,tid,eid,ref spatialId);}
            PatrolPath[] paths=e.GetComponentsInChildren<PatrolPath>(true);for(int i=0;i<paths.Length;i++)if(paths[i]!=null&&paths[i].transform.parent==e.transform){if(TryId(paths[i],used,out ushort pid))SceneMarkupCompiler.CompilePatrolPath(paths[i],d,pid);}
        }

        private static void BakeNavigation(GameObject root,MapDataset d,HashSet<ushort> used,ref ushort spatialId)
        {
            NavigationSurface[] surfaces=root.GetComponentsInChildren<NavigationSurface>(true);for(int i=0;i<surfaces.Length;i++)if(surfaces[i]!=null&&TryId(surfaces[i],used,out ushort id))SceneMarkupCompiler.CompileNavigationSurface(surfaces[i],d,id,ref spatialId);
            TraversalLink[] links=root.GetComponentsInChildren<TraversalLink>(true);for(int i=0;i<links.Length;i++)if(links[i]!=null&&TryId(links[i],used,out ushort id))SceneMarkupCompiler.CompileTraversalLink(links[i],d,id);
            PatrolPath[] paths=root.GetComponentsInChildren<PatrolPath>(true);for(int i=0;i<paths.Length;i++)if(paths[i]!=null&&paths[i].transform.parent!=null&&paths[i].GetComponentInParent<Encounter>()==null&&TryId(paths[i],used,out ushort id))SceneMarkupCompiler.CompilePatrolPath(paths[i],d,id);
        }
        private static void BakeActors(GameObject root,MapDataset d){Spawner[] s=root.GetComponentsInChildren<Spawner>(true);for(int i=0;i<s.Length;i++)if(s[i]!=null)d.Spawners.Add(new SpawnerRecord{HostId=s[i].Host.Id,Position=s[i].transform.position,EntityClass=s[i].entityClassToSpawn??string.Empty});Locator[] l=root.GetComponentsInChildren<Locator>(true);for(int i=0;i<l.Length;i++)if(l[i]!=null&&!(l[i] is Spawner))d.Locators.Add(new LocatorRecord{HostId=l[i].Host.Id,Position=l[i].transform.position,TagsJson=l[i].Facts!=null?l[i].Facts.ToJson():"{}"});}
        private static bool TryId(Object o,HashSet<ushort> used,out ushort id){string s=o is Component c?GetPath(c.transform):o.name;uint h=2166136261u;for(int i=0;i<s.Length;i++){h^=s[i];h*=16777619u;}id=(ushort)(h&0xFFFE);if(id==0)id=1;for(int i=0;i<65534;i++){if(!used.Contains(id)){used.Add(id);return true;}id++;if(id==0)id=1;}id=0;Debug.LogError($"[WorldPacker] ID exhaustion for {o.name}.",o);return false;}
        private static string GetPath(Transform t){var list=new List<string>();while(t!=null){list.Add(t.name);t=t.parent;}list.Reverse();return string.Join("/",list.ToArray());}
        private static void EnsureFolder(string folder){if(AssetDatabase.IsValidFolder(folder))return;string[] parts=folder.Split('/');string current=parts[0];for(int i=1;i<parts.Length;i++){string next=current+"/"+parts[i];if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(current,parts[i]);current=next;}}
    }
}
#endif
