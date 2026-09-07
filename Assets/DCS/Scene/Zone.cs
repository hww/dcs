using UnityEngine;

namespace DynamicComponent
{
    /// <summary>
    /// Zone — physical trigger area of the world map. Coordinates additive subscene 
    /// streaming passes and bootstrap cycles for isolated local Lua scripts.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Zone : Entity
    {
        [Header("Streaming Settings")]
        [Tooltip("The exact asset scene name to stream additively when player enters.")]
        [SerializeField] private string targetSceneName;

        [Tooltip("Relative path to the zone director script file from StreamingAssets/Lua root directory.")]
        [SerializeField] private string zoneDirectorScriptPath = "Locations/SwampZone/swamp_zone_director.lua";

        [Header("Legacy Detection Bounds (Gizmos compliance)")]
        public bool NeedCollision = true;
        public float ZoneRadius = 40f;
        public float ActivationRadius = 50f;

        [Header("Zone State Flags")]
        public bool IsZoneActive = false;

        private Collider _zoneCollider;

        protected virtual void Awake()
        {
            _zoneCollider = GetComponent<Collider>();
            _zoneCollider.isTrigger = true; // Enforce trigger setup for spatial tracking passes
        }

        // ============================================================
        //  ASYNC STREAMING ENGINE PASSES VIA PLAYER TRIGGER INTERACTION
        // ============================================================

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && DynamicComponent.Lua.ZoneManager.Instance != null)
            {
                SetActive(true);
                DynamicComponent.Lua.ZoneManager.Instance.LoadZoneAsync(targetSceneName, zoneDirectorScriptPath);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && DynamicComponent.Lua.ZoneManager.Instance != null)
            {
                SetActive(false);
                DynamicComponent.Lua.ZoneManager.Instance.UnloadZoneAsync(targetSceneName);
            }
        }

        public virtual void SetActive(bool state)
        {
            IsZoneActive = state;
            Debug.Log($"[Zone] {(state ? "Activated" : "Deactivated")}: {name}");
        }

        public virtual void SetActiveCamera(string cameraName)
        {
            Debug.Log($"[Zone] Activate camera '{cameraName}' inside streaming partition block: {name}");
        }

        // ============================================================
        //  UTILITIES & EDITOR COMPLIANCE
        // ============================================================

        public virtual bool ContainsPoint(Vector3 point)
        {
            return Vector3.Distance(transform.position, point) <= ZoneRadius;
        }

        public virtual float GetNormalizedDistance(Vector3 point)
        {
            float distance = Vector3.Distance(transform.position, point);
            return Mathf.Clamp01(distance / ZoneRadius);
        }

        private void OnDrawGizmos()
        {
            if (NeedCollision)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, ZoneRadius);

                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, ActivationRadius);
            }
        }

        public override void Inspect()
        {
            base.Inspect();
            Debug.Log($"[Zone Audit] Active: {IsZoneActive}, TargetScene: {targetSceneName}");
        }
    }
}
