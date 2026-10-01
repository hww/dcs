using DCS.Actors;
using DCS.Core;
using UnityEngine;

namespace DCS.Game.SoldierLua
{
    public static class SoldierFactory
    {
        public static Host Spawn(
            Domain domain,
            string prefabPath,
            string soldierName,
            Vector3 position)
        {
            // --- Prefab ---
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[SoldierFactory] Prefab not found: '{prefabPath}'");
                return Host.Null;
            }

            // --- Instantiate ---
            GameObject go = Object.Instantiate(prefab, position, Quaternion.identity);
            go.name = soldierName;

            var link = go.GetComponentInChildren<IHostReference>();
            if (link == null)
            {
                Debug.LogError($"[SoldierFactory] No IHostReference in '{prefabPath}'");
                Object.Destroy(go);
                return Host.Null;
            }

            // --- Host ---
            Host host = HostManager.CreateHost();
            HostManager.LinkHostReference(host, link);

            if (link is ILifeCycle lc)
                lc.Birth();

            var actor = link as Actor;
            Debug.Log($"[SoldierFactory] link={link.GetType().Name}, actor={(actor != null)}, " +
          $"actor.Animator={(actor?.Animator != null)}");
            // --- Prius ---
            var prius = new SoldierPrius
            {
                Host = host,
                Domain = domain,
                GameObject = go,
                Actor = actor,
                SoldierName = soldierName,
                SpawnPosition = position,
                Archetype = "Soldier",
            };

            // --- Position ---
            Handle hPos = DCSystem.Allocate<PositionComponent>(host, domain.HostChain);
            ref var pos = ref DCSystem.ResolveHandle<PositionComponent>(hPos);
            pos.Position = position;
            pos.Rotation = Quaternion.identity;
            prius.PositionHandle = hPos;

            // --- Name ---
            Handle hName = DCSystem.Allocate<NameComponent>(host, domain.HostChain);
            DCSystem.ResolveHandle<NameComponent>(hName).Name = soldierName;

            // --- Archetype ---
            Handle hArch = DCSystem.Allocate<ArchetypeComponent>(host, domain.HostChain);
            DCSystem.ResolveHandle<ArchetypeComponent>(hArch).Archetype = prius.Archetype;

            // --- Velocity ---
            DCSystem.ResolveHandle<VelocityComponent>(
                DCSystem.Allocate<VelocityComponent>(host, domain.HostChain)).Value = Vector3.zero;

            // --- Look ---
            Handle hLook = DCSystem.Allocate<LookComponent>(host, domain.HostChain);
            prius.LookHandle = hLook;

            // --- KeyboardInput (со ссылкой на Look) ---
            Handle hKb = DCSystem.Allocate<KeyboardInputComponent>(host, domain.HostChain);
            DCSystem.ResolveHandle<KeyboardInputComponent>(hKb).LookHandle = hLook;
            prius.KeyboardInputHandle = hKb;

            // --- AnimationState (Init возьмёт Animator из prius) ---
            Handle hAnim = DCSystem.Allocate<AnimationStateComponent>(
                host, domain.HostChain, prius);
            ref var anim = ref DCSystem.ResolveHandle<AnimationStateComponent>(hAnim);
            anim.PositionHandle = hPos;
            anim.LookHandle = hLook;
            anim.KeyboardInputHandle = hKb;
            prius.AnimationStateHandle = hAnim;

            // --- TransformState (Init возьмёт Actor из prius) ---
            Handle hTs = DCSystem.Allocate<TransformStateComponent>(
                host, domain.HostChain, prius);
            ref var ts = ref DCSystem.ResolveHandle<TransformStateComponent>(hTs);
            ts.PositionHandle = hPos;
            ts.LookHandle = hLook;
            prius.TransformStateHandle = hTs;


            var cam = Object.FindFirstObjectByType<LuaSoldierCamera>();
            if (cam != null && cam.Target == null && actor != null)
            {
                cam.Target = actor.transform;
            }

            return host;
        }
    }
}