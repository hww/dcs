using UnityEngine;

namespace DynamicComponent
{
    /// <summary>
    /// Types of interactive gameplay actors.
    /// </summary>
    public enum EntityActorType { Default, Collectible, Enemy, Player, Environment }

    /// <summary>
    /// Interactive Actor — an entity backed by physics representations and visual geometry.
    /// Inherits the base transactional Entity lifecycle without running heavy automatic C# processes.
    /// </summary>
    public class EntityActor : Entity
    {
        [Header("Entity Actor - Physics")]
        [Tooltip("Reference to the Rigidbody component (automatically resolved at birth).")]
        public Rigidbody RigidBody;

        [Tooltip("Reference to the Collider component (automatically resolved at birth).")]
        public Collider Collider;

        [Tooltip("Animator reference for driving object state-based animation parameters.")]
        public Animator Animator;

        [Header("Entity Actor - Type")]
        [Tooltip("Actor category flag used for logical filtering passes.")]
        public EntityActorType ActorType = EntityActorType.Default;

        [Header("Entity Actor - Visual")]
        [Tooltip("Visual mesh presentation representation (optional container anchor).")]
        public GameObject Visual;

        [Header("Entity Actor - Owners")]
        [Tooltip("The core level layout instance this actor belongs to.")]
        public Level Level;

        [Tooltip("The streamed logical gameplay zone context this actor belongs to.")]
        public Zone Zone;

        [Tooltip("The specific procedural spawner anchor that instantiated this actor.")]
        public Spawner Spawner;

        [Header("Entity Actor - Facts")]
        [Tooltip("The JSON-backed dynamic overlay metadata properties container (Naughty Dog TAGS style).")]
        public DynamicFacts Facts;

        [Header("Entity Actor - Find Tools")]
        [Tooltip("Utility helper to scan, cache, and verify animator controller states in the editor.")]
        public AnimatorFinder AnimatorFinder;

        public string DisplayName;
        // ============================================================
        //  EARLY INITIALIZATION
        // ============================================================

        private void Awake()
        {
            InitializeComponents();
        }

        /// <summary>
        /// Collects and binds required native Unity component anchors to the actor context.
        /// </summary>
        protected virtual void InitializeComponents()
        {
            InitializeAnimator();
            InitializePhysics();
            InitializeVisual();
        }

        protected virtual void InitializePhysics()
        {
            RigidBody = GetComponent<Rigidbody>();
            Collider = GetComponent<Collider>();

            if (RigidBody == null)
                Debug.LogWarning($"[EntityActor] Runtime warning: {name} has no Rigidbody attached.", this);

            if (Collider == null)
                Debug.LogWarning($"[EntityActor] Runtime warning: {name} has no Collider attached.", this);
        }

        protected virtual void InitializeVisual()
        {
            if (Visual == null)
            {
                var meshFilter = GetComponentInChildren<MeshFilter>();
                if (meshFilter != null)
                    Visual = meshFilter.gameObject;
            }
        }

        protected virtual void InitializeAnimator()
        {
            Animator = GetComponent<Animator>();
            AnimatorFinder = new AnimatorFinder(Animator);
        }

        // ============================================================
        //  OVERRIDDEN DATA-DRIVEN LIFECYCLE CONTOUR
        // ============================================================

        /// <summary>
        /// Triggered automatically when the subscene section is streamed or spawned additively.
        /// Generates EntityID (DCS Host.Id) and wires the actor straight into the flat zone registry.
        /// </summary>
        public override void Birth()
        {
            // Triggers baseline C# logic and unique number ID allocation
            base.Birth();

            InitializePhysics();
            InitializeVisual();

            // AUTOMATIC REGISTRATION: Insert into the flat lookup directory of the streaming engine
            if (DynamicComponent.Lua.ZoneManager.Instance != null)
            {
                // Register using lowered case to guarantee absolute string match invariants
//DynamicComponent.Lua.ZoneManager.Instance.RegisterActiveActor(name.ToLower(), this);
            }
        }

        /// <summary>
        /// Triggered automatically when the parent scene partition unloads from memory.
        /// Safely strips execution registry tracking handles to prevent memory leaks.
        /// </summary>
        public override void Kill()
        {
            // Clear flat registry runtime tracking footprint mappings
            if (DynamicComponent.Lua.ZoneManager.Instance != null)
            {
               // DynamicComponent.Lua.ZoneManager.Instance.UnregisterActiveActor(name.ToLower());
            }

            base.Kill();
        }

        /// <summary>
        /// Initialization fallback method overridden to preserve legacy pipeline compliance passes.
        /// </summary>
        public override void InitByProcess(Host process)
        {
            InitializeComponents();
            base.InitByProcess(process);
        }

        // ============================================================
        //  DIAGNOSTICS & EDITOR TOOLS
        // ============================================================

        /// <summary>
        /// Prints detailed configuration metrics layout state into the Unity console.
        /// </summary>
        public override void Inspect()
        {
            base.Inspect();
            Debug.Log($"[EntityActor Details] ActorType: {ActorType}, Physics: {RigidBody != null}, " +
                     $"Collider: {Collider != null}, Visual: {Visual != null}, EntityID (DCS Host.Id): {EntityID}");
        }
    }
}
