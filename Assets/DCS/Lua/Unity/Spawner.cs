using UnityEngine;

namespace DynamicComponent
{
    /// <summary>
    /// Spawner — passive spawn configuration point data anchor.
    /// Contains target attributes read by Top-Down Lua script directors to allocate DCS entities.
    /// </summary>
    public class Spawner : Locator
    {
        public enum ESpawnMode
        {
            SpawnByCode,        // Allocated strictly via execution requests from Lua script code
            SpawnOnLoadScene,   // Automated baseline allocation on scene load sequence triggers
            SpawnOnSelectZone,  // Bootstrapped during spatial zone selection routines
            AutoSpawn           // Script-driven polling evaluation
        }

        [System.Serializable]
        public struct SpawnerSize
        {
            public float radius;
            public float deSpawnRadius;
        }

        [Header("DCS Allocation Rules metadata")]
        public ESpawnMode Mode = ESpawnMode.SpawnByCode;
        public SpawnerSize Size = new SpawnerSize { radius = 10, deSpawnRadius = 100 };

        [Header("Hierarchy Integration Settings")]
        public string ParentToEntity;
        public string ParentToJoint;

        [Header("Script Startup Overlays (Naughty Dog framework style)")]
        public string InitialState;
        public string[] InitialArguments;

        [Header("Child Data Nodes")]
        public GameObject[] ChildNodes;

        public override void Birth()
        {
            base.Birth();
            Debug.Log($"[Spawner Data Node] Birth: '{name}' allocated in mode {Mode}. EntityID (Host.Id) = {EntityID}");
        }

        // Execution paths removed from C# layout — allocation operations live entirely inside script logic
        public virtual void Spawn()
        {
            Debug.Log($"[Spawner Bridge] Forwarding spawn directive context via top-down Lua systems for: {name}");
        }

        public virtual void Despawn()
        {
            Debug.Log($"[Spawner Bridge] Forwarding despawn cleanup pass down into active local scope for: {name}");
        }

        public bool IsInSpawnRadius(Vector3 position)
        {
            return Vector3.Distance(transform.position, position) <= Size.radius;
        }

        public bool IsOutsideDespawnRadius(Vector3 position)
        {
            return Vector3.Distance(transform.position, position) > Size.deSpawnRadius;
        }

        private void OnDrawGizmos()
        {
            if (Mode == ESpawnMode.AutoSpawn)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, Size.radius);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, Size.deSpawnRadius);
            }
        }
    }
}
