using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

namespace DynamicComponent
{
    // ============================================================
    //  COMPONENT TYPE — TYPE ID GENERATOR
    // ============================================================

    /// <summary>
    /// Auto-generates and registers a unique type ID for each component type.
    /// </summary>
    /// <typeparam name="T">Component type to register.</typeparam>
    /// <remarks>
    /// The static constructor is triggered by RuntimeHelpers.RunClassConstructor
    /// during pool initialization, registering the type and creating its pool.
    ///
    /// This design ensures:
    /// - Type IDs are assigned at compile-time (via static constructor)
    /// - No runtime reflection in hot paths
    /// - Type-safe access to pools
    ///
    /// Usage: ComponentType{MyComponent}.Id returns the unique type ID.
    /// </remarks>
    public static class ComponentType<T> where T : struct
    {
        /// <summary>Unique type identifier for T.</summary>
        public static readonly int Id = ComponentRegistry.RegisterNewType<T>();
    }

    // ============================================================
    //  COMPONENT REGISTRY — CENTRAL TYPE AND POOL REGISTRY
    // ============================================================

    /// <summary>
    /// Central registry for all component types and their pools.
    /// </summary>
    /// <remarks>
    /// Manages:
    /// - Unique type IDs (assigned during static initialization)
    /// - Pool instances for each component type
    /// - Event type tracking for polling
    ///
    /// Initialization flow:
    /// 1. ComponentType{T}.Id triggers RegisterNewType{T}
    /// 2. RegisterNewType creates the appropriate pool
    /// 3. InitializeAllPools scans assemblies for DcsPoolAttribute
    /// 4. Forces static constructor execution for all registered types
    ///
    /// This design provides O(1) pool access by TypeId.
    /// </remarks>
    public static class ComponentRegistry
    {
        /// <summary>Internal type counter for assigning unique IDs.</summary>
        private static int _typeCounter = 0;

        /// <summary>Maximum number of component types.</summary>
        public const int MaxComponentTypes = 200;

        /// <summary>Array of all component pools, indexed by TypeId.</summary>
        public static readonly IComponentPool[] Pools = new IComponentPool[MaxComponentTypes];

        /// <summary>Array of event type IDs for polling.</summary>
        public static readonly int[] PollTypeIds = new int[MaxComponentTypes];

        /// <summary>Number of registered event types.</summary>
        public static int PollTypesCount = 0;

        /// <summary>
        /// Initializes all pools by scanning assemblies for DcsPoolAttribute.
        /// </summary>
        /// <remarks>
        /// This must be called once at startup (e.g., in the game initialization).
        /// It:
        /// 1. Scans all types in the executing assembly
        /// 2. Finds structs with DcsPoolAttribute
        /// 3. Triggers their static constructor via RuntimeHelpers.RunClassConstructor
        /// 4. Collects event types into PollTypeIds for fast iteration
        ///
        /// After this call, all pools are ready for use.
        /// </remarks>
        public static void InitializeAllPools()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();

            foreach (var type in types)
            {
                var poolAttribute = type.GetCustomAttribute<BasePoolAttribute>();

                if (type.IsValueType && poolAttribute != null)
                {
                    // Force static constructor execution to register the type
                    var genericComponentType = typeof(ComponentType<>).MakeGenericType(type);
                    RuntimeHelpers.RunClassConstructor(genericComponentType.TypeHandle);

                    // If it's an event type, add it to the polling list
                    if (typeof(IEvent).IsAssignableFrom(type))
                    {
                        var idField = genericComponentType.GetField(
                            "Id",
                            BindingFlags.Public | BindingFlags.Static
                        );
                        PollTypeIds[PollTypesCount++] = (int)idField.GetValue(null);
                    }
                }
            }

            Debug.Log($"<color=green>[DCS SUCCESS]</color> Pools allocated. Total types: {_typeCounter}");
        }

        /// <summary>
        /// Registers a new component type and creates its pool.
        /// </summary>
        /// <typeparam name="T">Component type to register.</typeparam>
        /// <returns>Unique TypeId for T.</returns>
        /// <exception cref="System.Exception">If the type limit is exceeded.</exception>
        /// <remarks>
        /// Called automatically by ComponentType{T}.Id static constructor.
        ///
        /// Algorithm:
        /// 1. Assigns a new TypeId
        /// 2. Reads DcsPoolAttribute for capacity and settings
        /// 3. Creates either:
        ///    - EventManager{T} if T implements IEventData
        ///    - ComponentManager{T} for regular components
        /// 4. Stores the pool in the Pools array at the TypeId index
        ///
        /// Complexity: O(1)
        /// </remarks>
        public static int RegisterNewType<T>() where T : struct
        {
            int newId = _typeCounter++;
            if (newId >= MaxComponentTypes)
                throw new System.Exception("DCS Error: Component type limit exceeded!");

            // Get capacity from attribute or use default
            int capacity = 1000;
            var attr = typeof(T).GetCustomAttribute<BasePoolAttribute>();
            if (attr != null)
                capacity = attr.Capacity;

            // Create the appropriate pool type
            if (typeof(IEvent).IsAssignableFrom(typeof(T)))
            {
                var eventManagerType = typeof(EventPool<>).MakeGenericType(typeof(T));
                Pools[newId] = (IComponentPool)System.Activator.CreateInstance(eventManagerType, capacity);
            }
            else
            {
                Pools[newId] = new ComponentPool<T>(capacity);
            }

            return newId;
        }

        /// <summary>
        /// Gets the typed pool for component type T.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <returns>Typed component manager for T.</returns>
        /// <remarks>
        /// This is the primary way to access pools in hot paths.
        /// Uses aggressive inlining for zero-overhead access.
        ///
        /// Complexity: O(1)
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentPool<T> GetPool<T>() where T : struct
        {
            return (ComponentPool<T>)Pools[ComponentType<T>.Id];
        }

        /// <summary>
        /// Gets the total number of registered component types in the system.
        /// </summary>
        public static int GetTypesCount()
        {
            return _typeCounter;
        }

        /// <summary>
        /// Resolves a component structure name by its unique integer TypeId.
        /// </summary>
        /// <param name="id">The unique component TypeId.</param>
        /// <returns>The string name of the component struct type, or an empty string if invalid.</returns>
        public static string GetTypeNameById(int id)
        {
            if (id < 0 || id >= _typeCounter || Pools[id] == null)
            {
                return string.Empty;
            }

            System.Type poolType = Pools[id].GetType();
            if (poolType.IsGenericType)
            {
                System.Type[] genericArgs = poolType.GetGenericArguments();
                if (genericArgs.Length > 0)
                {
                    return genericArgs[0].Name;
                }
            }

            return string.Empty;
        }

    }
}