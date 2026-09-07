using DynamicComponent;
using UnityEngine;

namespace DynamicComponent
{
    /// <summary>
    /// Base abstract game entity. Responsible strictly for stable structural identity management
    /// and providing baseline unified lifecycle hooks for scene components.
    /// </summary>
    public abstract class Entity : MonoBehaviour
    {
        [Header("Entity Base Metadata")]
        [Tooltip("The unique integer runtime identifier. Maps 1:1 onto the DCS Host.Id layout.")]
        [SerializeField] protected int _entityId;

        // Tracks the global monotonic integer counter to guarantee absolute ID uniqueness
        private static int _lastAllocatedId;

        /// <summary>
        /// Unique integer identifier of this entity context. Translates straight into a DCS Host Handle.
        /// </summary>
        public int EntityID => _entityId;

        /// <summary>
        /// Verified state configuration flag indicating if the entity is currently active in the world scene.
        /// </summary>
        public bool IsAlive { get; private set; }

        // ============================================================
        //  NATIVE UNITY LIFECYCLE HOOKS MAPPING
        // ============================================================

        protected virtual void OnEnable()
        {
            Birth();
        }

        protected virtual void OnDisable()
        {
            Kill();
        }

        // ============================================================
        //  CORE LIFECYCLE VIRTUAL METHODS
        // ============================================================

        /// <summary>
        /// Birth phase execution. Allocates a unique integer identifier 
        /// and wires the object straight into the data-driven framework directory.
        /// </summary>
        public virtual void Birth()
        {
            _entityId = GenerateUniqueId();
            IsAlive = true;
            Debug.Log($"[Entity] Birth: Node initialized -> '{name}' wrapped with EntityID: {_entityId}");
        }

        /// <summary>
        /// Kill phase execution. Strips tracking entries and handles cleanup routines.
        /// </summary>
        public virtual void Kill()
        {
            IsAlive = false;
            Debug.Log($"[Entity] Kill: Node destroyed -> '{name}' stripped from EntityID: {_entityId}");
        }

        // ============================================================
        //  LEGACY PARADIGM COMPLIANCE STUBS (SAFE TO DEPRECATE)
        // ============================================================

        // Kept temporarily to prevent direct compilation breakage in dependent classes.
        // In full Data-Driven architecture, these processes are handled natively inside Lua.
        public virtual void InitByProcess(Host process) { }

        // ============================================================
        //  UTILITY GENERATORS
        // ============================================================

        /// <summary>
        /// Monotonically generates a unique runtime integer identifier.
        /// </summary>
        private static int GenerateUniqueId()
        {
            return ++_lastAllocatedId;
        }

        /// <summary>
        /// Basic debug string conversion override format.
        /// </summary>
        public override string ToString()
        {
            return $"{name} (EntityID: {_entityId}, IsAlive: {IsAlive})";
        }

        /// <summary>
        /// Prints explicit structural debugging specifications to the Unity log.
        /// </summary>
        public virtual void Inspect()
        {
            Debug.Log($"[Entity Audit] Instance Name: '{name}', EntityID (DCS Host.Id): {_entityId}, IsAlive: {IsAlive}");
        }
    }
}
