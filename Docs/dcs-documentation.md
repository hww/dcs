# Dynamic Component System (DCS)

High-performance ECS-like component system for Unity with dense array storage, sparse roster handles, and event-driven communication.

## Architecture Overview

**Core concepts:**

- **Host** – Owner of components (game object, NPC, bullet). Lightweight ID with generation.
- **Component** – Data stored in dense arrays. Accessed via handles with generation validation.
- **Handle** – Safe reference (ID + Generation). Stale handles return `IsNull == true`.
- **Pool** – Manages components of a single type. Dense array + sparse roster with Swap-Back compaction.

**Memory layout:**

- Dense array: Contiguous component data (cache-friendly iteration)
- Sparse roster: Stable handles with generation (safe references)
- Swap-Back: O(1) removal with compaction

**Key design choices:**

- Zero allocations in hot paths
- No virtual calls or boxing
- Generation-based handle validation
- Two-phase event dispatching

---

## Host Management

Host is the owner of components. Created/destroyed via `HostManager`.

```csharp
// Create a host
Host host = HostManager.CreateHost();

// Check validity
if (HostManager.IsValid(host)) { /* ... */ }

// Destroy host and all its components
HostManager.DestroyHost(host, chainManager);
```

**HostData structure:**

```csharp
public struct HostData {
    public int Id;                 // Unique identifier
    public int Generation;         // Incremented on destroy
    public int FirstComponent;     // Head of component chain
    public int Next;               // Free list link
}
```

---

## Component Pools

### ComponentPool<T>

Base pool with dense array storage and sparse roster handles.

```csharp
// Define a component
public struct Health : IComponent {
    public int RosterIndex { get; set; }
    public int Value;
}

// Allocate
Handle healthHandle = DCS.Allocate<Health>(host, chain);

// Resolve and modify
ref Health health = ref DCS.ResolveHandle<Health>(healthHandle);
health.Value = 100;

// Free
DCS.Free<Health>(host, chain, ref healthHandle);
```

**ComponentPool<T> key members:**

```csharp
public class ComponentPool<T> : IComponentPool where T : struct {
    public int Partition;          // Active count boundary
    public T[] Components;         // Dense array
    public RosterItem[] Roster;    // Sparse roster
    
    public Handle Allocate(Host host, HostChain chain, object prius = null);
    public ref T ResolveHandle(Handle handle);
    public void Free(Host host, HostChain chain, ref Handle handle);
    public void ClearFramePool();  // Reset for frame-based pools
}
```

### EventPool<T>

Specialized pool for event components (short-lived, frame-based).

```csharp
public struct DamageEvent : IEvent {
    public int RosterIndex { get; set; }
    public uint NamespaceMask { get; set; }
    public int DamageAmount;
}

// Allocate event
EventPool<DamageEvent> eventPool = (EventPool<DamageEvent>)ComponentRegistry.Pools[ComponentType<DamageEvent>.Id];
Handle eventHandle = eventPool.AllocateEvent(host, namespaceMask, chain);

// Access event data
ref DamageEvent ev = ref eventPool.ResolveHandle(eventHandle);
ev.DamageAmount = 50;
```

**EventPool<T> key members:**

```csharp
public class EventPool<T> : ComponentPool<T> where T : struct, IEvent {
    public Handle AllocateEvent(Host host, uint namespaceMask, HostChain chain);
    public void SystemPoll(EventSubscription subManager, TypeChain typeChain);
}
```

---

## Handle System

### Handle

Safe reference to a component. Validated by generation.

```csharp
public struct Handle {
    public int Id;           // Roster index
    public int Generation;   // Incremented on each free
    public bool IsNull => Generation == 0;
}
```

### TypedHandle

Handle with type information for event dispatching.

```csharp
public struct TypedHandle {
    public int TypeId;       // Component type ID
    public int Id;           // Roster index
    public int Generation;
    public bool IsNull => Generation == 0;
}
```

### Host

Safe reference to a host.

```csharp
public struct Host {
    public int Id;
    public int Generation;
    public bool IsNull => Generation == 0;
}
```

---

## Component Chain (HostChain)

Linked list of components belonging to a host. Enables fast iteration and type lookup.

```csharp
public class HostChain {
    public const int MaxComponents = 500000;
    
    public void Add(Host host, Handle component, int typeId);
    public void Remove(Host host, Handle component, int typeId);
    public bool Contains(Host host, Handle component, int typeId);
    public ChainNode GetTypedHandle(Host host, int typeId);
    public void FreeChain(Host host);  // Free all components of a host
}
```

**ChainNode structure:**

```csharp
public struct ChainNode {
    public Handle Component;
    public int TypeId;
    public int Next;  // -1 = end of chain
}
```

---

## Type System & Registry

### ComponentType<T>

Auto-generates unique type ID for each component type.

```csharp
// Type ID assigned at compile-time via static constructor
int typeId = ComponentType<Health>.Id;
```

### ComponentRegistry

Central registry for all component types and their pools.

```csharp
public static class ComponentRegistry {
    public const int MaxComponentTypes = 200;
    public static readonly IComponentPool[] Pools;
    public static readonly int[] PollTypeIds;     // Event types for polling
    public static int PollTypesCount;
    
    public static void InitializeAllPools();      // Scan assemblies for attributes
    public static ComponentPool<T> GetPool<T>() where T : struct;
}
```

**Attributes for pool configuration:**

```csharp
[ComponentPool(capacity = 1000, updateStage = EUpdateStage.Update)]
public struct Health : IComponent { /* ... */ }

[MessagePool(capacity = 500, updateStage = EUpdateStage.Update)]
public struct DamageEvent : IEvent { /* ... */ }
```

---

## Event System

### Subscription

Links a process (FSM) to an event type.

```csharp
public struct SubscriptionNode : IComponent {
    public int TargetEventTypeId;    // Event type being subscribed to
    public Handle ProcessHandle;     // Receiver process
    public int ProcessTypeId;        // Receiver pool type
    public uint NamespaceMask;       // Filtering mask
    public int RosterIndex { get; set; }
}
```

### EventSubscription

Manages subscriptions. Component pool for `SubscriptionNode`.

```csharp
public class EventSubscription : ComponentPool<SubscriptionNode> {
    public Handle AllocateSubscription<TEvent, TProcess>(
        Host receiverHost,
        Handle receiverProcessHandle,
        uint namespaceMask,
        HostChain hostChain,
        TypeChain typeChain
    ) where TEvent : struct, IEvent
      where TProcess : struct, IComponent, IMessageReceiver;
    
    public void FreeSubscription(Host host, HostChain hostChain, TypeChain typeChain, ref Handle handle);
}
```

### TypeChain

Manages subscription chains organized by event type.

```csharp
public class TypeChain {
    public const int MaxTypeNodes = 100000;
    
    public void Add(int eventTypeId, Handle subHandle, int processTypeId);
    public void Remove(int eventTypeId, Handle subHandle);
    public int GetTypeChainHead(int eventTypeId);
    public ref TypeChainNode GetNode(int index);
}
```

**TypeChainNode:**

```csharp
public struct TypeChainNode {
    public Handle SubscriptionHandle;
    public int ProcessTypeId;
    public int Next;  // -1 = end of chain
}
```

### EventSystem

Two-phase event dispatching with zero allocation.

```csharp
public static class EventSystem {
    public const int MaxInvokes = 1000;
    
    // Phase A: Collect events
    public static void PollEvents<TEvent>(EventSubscription subManager, TypeChain typeChain)
        where TEvent : struct, IEvent;
    
    // Phase B: Deliver all collected events
    public static void DeliverEvents(HostChain chain);
}
```

**InvokeRecord:**

```csharp
public struct InvokeRecord {
    public Host ReceiverHost;
    public Handle ReceiverProcessHandle;
    public int ReceiverProcessTypeId;
    public int EventTypeId;
    public int ComponentId;          // denseIndex of event
    public int ComponentGeneration;
}
```

### IEventDispatcher

Interface for event pools to integrate with event system.

```csharp
public interface IEventDispatcher {
    void SystemPoll(EventSubscription subManager, TypeChain typeChain);
}
```

### IMessageReceiver

Interface for components that can receive messages.

```csharp
public interface IMessageReceiver {
    void ReceiveMessage(int msgTypeId, Handle msgHandle);
}
```

**Example: Process receiving events**

```csharp
public struct EnemyFSM : IComponent, IMessageReceiver {
    public int RosterIndex { get; set; }
    public int State;
    
    public void ReceiveMessage(int msgTypeId, Handle msgHandle) {
        if (msgTypeId == ComponentType<DamageEvent>.Id) {
            ref DamageEvent ev = ref DCS.ResolveHandle<DamageEvent>(msgHandle);
            // Handle damage
        }
    }
}
```

### Event Flow Example

```csharp
// 1. Define event
public struct DamageEvent : IEvent {
    public int RosterIndex { get; set; }
    public uint NamespaceMask { get; set; }
    public int Amount;
}

// 2. Subscribe to event
Handle subHandle = subManager.AllocateSubscription<DamageEvent, EnemyFSM>(
    host,
    processHandle,
    namespaceMask: 0x0001,
    hostChain,
    typeChain
);

// 3. Allocate and fire event
Handle eventHandle = eventPool.AllocateEvent(host, 0x0001, hostChain);
ref DamageEvent ev = ref eventPool.ResolveHandle(eventHandle);
ev.Amount = 50;

// 4. Event system processes in Update phase
DCS.UpdateComponents(
    EUpdateStage.Update,
    scheduler,
    subManager,
    typeChain,
    hostChain
);
```

---

## Update Scheduler

Manages update order priorities for component types.

```csharp
public sealed class UpdateScheduler {
    public void SetOrder<T>(int order) where T : struct;  // Lower = earlier
    public int[] GetSortedTypes();                        // Lazy rebuild
    public int GetOrder(int typeId);
    public void RemoveOrder<T>() where T : struct;
    public void ClearAllOrders();
    public void ForceRebuild();
}
```

**Usage:**

```csharp
var scheduler = new UpdateScheduler();

// Set execution order
scheduler.SetOrder<DamageEvent>(-10);   // Process damage events first
scheduler.SetOrder<HealEvent>(10);      // Heal events later
scheduler.SetOrder<DeathEvent>(20);     // Death events last

// Get sorted types for event processing
int[] sortedTypes = scheduler.GetSortedTypes();
// Types with Order = -10 first, then default (0), then Order = 10, etc.
```

---

## Update Stages

```csharp
public enum EUpdateStage {
    None,
    Update,       // Main frame update (Time.deltaTime)
    FixedUpdate,  // Fixed timestep (Time.fixedDeltaTime)
    PostUpdate    // Post-render update
}

public enum EAsyncUpdateStage {
    None,
    Update,
    FixedUpdate,
    PostUpdate
}
```

### DCS.UpdateComponents

Main entry point for event processing.

```csharp
public static void UpdateComponents(
    EUpdateStage stage,
    UpdateScheduler scheduler,
    EventSubscription subManager,
    TypeChain typeChain,
    HostChain chain,
    uint mask = 0
)
{
    int[] sortedTypes = scheduler.GetSortedTypes();
    
    switch (stage) {
        case EUpdateStage.Update:
            // Phase A: Collect events in priority order
            for (int i = 0; i < sortedTypes.Length; i++) {
                int eventTypeId = sortedTypes[i];
                if (ComponentRegistry.Pools[eventTypeId] is IEventDispatcher dispatcher) {
                    dispatcher.SystemPoll(subManager, typeChain);
                }
            }
            // Phase B: Deliver all collected events
            EventSystem.DeliverEvents(chain);
            break;
            
        case EUpdateStage.PostUpdate:
            // Clear all frame-based event pools
            for (int i = 0; i < sortedTypes.Length; i++) {
                ComponentRegistry.Pools[sortedTypes[i]].ClearFramePool();
            }
            break;
    }
}
```

---

## Interfaces

### IComponent

Required for all components stored in pools.

```csharp
public interface IComponent {
    int RosterIndex { get; set; }
}
```

### IInitializable

Optional initialization via `prius` object.

```csharp
public interface IInitializable {
    void Init(object prius);
}
```

### IEvent

Marker interface for event components. Includes namespace mask for filtering.

```csharp
public interface IEvent : IComponent {
    uint NamespaceMask { get; set; }
}
```

### IMessageReceiver

Components that can receive events.

```csharp
public interface IMessageReceiver {
    void ReceiveMessage(int msgTypeId, Handle msgHandle);
}
```

### IComponentPool

Non-generic pool interface for lifecycle management.

```csharp
public interface IComponentPool {
    void SystemFree(Host hostHandle, HostChain chain, Handle handle);
    void ClearFramePool();
    void SystemDeliver(int rosterIndex, int msgTypeId, Handle msgHandle);
}
```

### IEventDispatcher

Event pools that can be polled.

```csharp
public interface IEventDispatcher {
    void SystemPoll(EventSubscription subManager, TypeChain typeChain);
}
```

---

## DCS Public API

Main entry point for game code.

```csharp
public static class DCS {
    // Get first component of type T from host
    public static Handle Get<T>(Host host, HostChain chain) where T : struct;
    
    // Allocate component
    public static Handle Allocate<T>(Host host, HostChain chain) where T : struct;
    
    // Resolve handle to component reference
    public static ref T ResolveHandle<T>(Handle handle) where T : struct;
    
    // Free component
    public static void Free<T>(Host host, HostChain chain, ref Handle handle) where T : struct;
    
    // Free all components of a host
    public static void FreeChain(Host host, HostChain chain);
    
    // Update event system
    public static void UpdateComponents(
        EUpdateStage stage,
        UpdateScheduler scheduler,
        EventSubscription subManager,
        TypeChain typeChain,
        HostChain chain,
        uint mask = 0
    );
}
```

---

## Complete Usage Example

```csharp
// === SETUP (once at startup) ===
ComponentRegistry.InitializeAllPools();

var hostChain = new HostChain();
var typeChain = new TypeChain();
var subManager = new EventSubscription(1000);
var scheduler = new UpdateScheduler();

// === CREATE HOST ===
Host player = HostManager.CreateHost();

// === ALLOCATE COMPONENTS ===
Handle healthHandle = DCS.Allocate<Health>(player, hostChain);
ref Health health = ref DCS.ResolveHandle<Health>(healthHandle);
health.Value = 100;

Handle posHandle = DCS.Allocate<Position>(player, hostChain);
ref Position pos = ref DCS.ResolveHandle<Position>(posHandle);
pos.X = 10.0f;

// === ALLOCATE PROCESS WITH EVENT SUBSCRIPTION ===
Handle fsmHandle = DCS.Allocate<EnemyFSM>(player, hostChain);

// Subscribe to DamageEvent
Handle subHandle = subManager.AllocateSubscription<DamageEvent, EnemyFSM>(
    player,
    fsmHandle,
    namespaceMask: 0x0001,
    hostChain,
    typeChain
);

// === FIRE EVENT ===
var eventPool = (EventPool<DamageEvent>)ComponentRegistry.GetPool<DamageEvent>();
Handle eventHandle = eventPool.AllocateEvent(player, 0x0001, hostChain);
ref DamageEvent ev = ref eventPool.ResolveHandle(eventHandle);
ev.Amount = 50;

// === PROCESS EVENTS ===
DCS.UpdateComponents(EUpdateStage.Update, scheduler, subManager, typeChain, hostChain);

// === DESTROY ===
HostManager.DestroyHost(player, hostChain);
```

---

## Performance Characteristics

| Operation | Complexity | Notes |
| ----------- | ------------ | ------- |
| Allocate component | O(1) | Dense allocation + roster setup |
| Resolve handle | O(1) | Single array lookup + generation check |
| Free component | O(1) + O(N)* | Swap-Back compaction + chain removal (*N = components of host) |
| Get component by type | O(N) | Scans host's component chain |
| Poll events | O(E × S) | E = events, S = subscriptions per event type |
| Deliver events | O(I) | I = number of invocations |
| Host creation | O(1) | Free list allocation |
| Host destruction | O(N) | N = components of host |

**Memory:**

- ComponentPool<T>: `capacity × (sizeof(T) + 16)` bytes
- HostChain: `500,000 × 24 = 12 MB`
- TypeChain: `100,000 × 24 = 2.4 MB`
- HostManager: `100,000 × 16 = 1.6 MB`

---

## Thread Safety

**Not thread-safe.** All operations must be on the main thread. No concurrent access from multiple threads.
