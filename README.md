# Dynamic Component System (DCS)

↪ **High-Performance Component Architecture for Unity Games**

DCS is an implementation of a dynamic component system inspired by the architecture used at Insomniac Games for titles like Ratchet & Clank. The system is designed for maximum performance on modern CPUs through Data-Oriented Design, dense memory pools, and a safe handle-based reference model.

---

## 📌 Key Features

- **Dense Pools (Dense Arrays)** — Components of the same type are stored contiguously, ensuring ideal data locality and efficient cache utilization.
- **Sparse Roster with Generational Handles** — Safe references to components with protection against dangling pointers (use-after-free).
- **Swap-Back Deletion** — O(1) component deallocation with automatic pool defragmentation.
- **Two-Phase Event System** — Zero-allocation message delivery through event pools and subscriptions.
- **Declarative Configuration** — Capacity and update phase management via C# attributes.
- **Global Masking** — Instant disable of entire component categories (pause, object types) with a single bitwise operation.
- **Multi-Threading Ready** — Separation of synchronous (PPU) and asynchronous (Job System) updates.

## 📌 Current Status: Core Complete, System in Development

This repository contains the **core DCS implementation** — a high-performance 
component pool architecture with event system and generational handles.

**What's implemented:**
- Component pools with dense arrays and sparse roster
- Safe handle system with generation validation
- Event system with subscriptions and two-phase delivery
- Host management and component chains

**Next steps (in progress):**
- State machine system built on top of DCS
- Process-oriented behavior system with coroutine-based sequencing
- Full DOD adaptation for high-level game logic

This is a living project. Contributions and feedback are welcome!

---

## 🏗️ Architecture at a Glance


```txt

┌────────────────────────────────────────────────────┐
│                       HOST                         │
│                 (Component Owner)                  │
└────────────────────────────────────────────────────┘
                          │
           ┌──────────────┴───────────────┐
           ▼                              ▼
┌─────────────────────┐        ┌─────────────────────┐
│   DATA CONTOUR      │        │   EVENT CONTOUR     │
│ Static Components   │        │ Dynamic Events      │
│ (Health, Position)  │        │ (DamageEvent,       │
│ Linear Iteration    │        │  Subscription)      │
└─────────────────────┘        └─────────────────────┘

```

**Host (Entity)** — A lightweight identifier that links all components in a chain.

**Components** — `struct` data stored in dense pools.

**Events** — Also components with a short lifecycle (typically 1 frame), delivered through the subscription system.

---

## 🧩 Core API

```csharp
// Create a host
Host player = HostManager.CreateHost();

// Allocate a component
Handle healthHandle = DCS.Allocate<Health>(player, hostChain);

// Access data
ref Health health = ref DCS.ResolveHandle<Health>(healthHandle);
health.Value = 100;

// Create an event
Handle damageHandle = eventPool.AllocateEvent(player, 0x0001, hostChain);
ref DamageEvent ev = ref eventPool.ResolveHandle(damageHandle);
ev.Amount = 50;

// Subscribe to an event
Handle subHandle = subManager.AllocateSubscription<DamageEvent, EnemyFSM>(
    player, fsmHandle, 0x0001, hostChain, typeChain
);

// Process events (called in Update)
DCS.UpdateComponents(EUpdateStage.Update, scheduler, subManager, typeChain, hostChain);

// Destroy host with all components
HostManager.DestroyHost(player, hostChain);
```

---

## 📊 Performance Characteristics

| Operation | Complexity | Notes |
| ----------- | ------------ | ------- |
| Allocate component | O(1) | Dense allocation + roster setup |
| Resolve handle | O(1) | Generation check + array access |
| Free component | O(1) + O(N)* | Swap-Back + chain removal (*host components) |
| Get component by type | O(N) | Scans host's component chain |
| Poll events | O(E × S) | E = events, S = subscriptions per event type |
| Deliver events | O(I) | I = number of invocations |

**Memory:**

- `ComponentPool<T>`: `capacity × (sizeof(T) + 16)` bytes
- `HostChain`: up to 500,000 nodes × 24 bytes ≈ 12 MB
- `TypeChain`: up to 100,000 nodes × 24 bytes ≈ 2.4 MB

---

## 📂 Repository Structure

```
├── README.md                           # This file
├── docs/
│   └── dcs-documentation.md           # Complete API technical documentation
├── analysis/
│   ├── manifest.md                    # Unity adaptation manifest (C# limitations)
│   └── unity-version.md               # Architectural reconstruction and commentary
└── src/                               # System source code
    ├── Core/
    │   ├── HostManager.cs
    │   ├── ComponentPool.cs
    │   └── HostChain.cs
    ├── Events/
    │   ├── EventPool.cs
    │   ├── EventSubscription.cs
    │   └── EventSystem.cs
    └── Attributes/
        └── PoolAttributes.cs
```

---

## 📖 Documentation

| Document | Description |
| ---------- | ------------- |
| **[Full API Documentation](Docs/dcs-documentation.md)** | Detailed description of all classes, structs, and system methods |
| **[Unity Adaptation Manifest](https://hww.github.io/articles/2013/terrance_cohen_dcs/manifest)** | Analysis of C# limitations and architectural decisions |
| **[Architectural Reconstruction](https://hww.github.io/articles/2013/terrance_cohen_dcs/unity-version)** | Deep dive into the original DCS from Insomniac Games |

---

## 🔗 Source Materials

- [Original Presentation by Terrance Cohen](https://hww.github.io/articles/2013/terrance_cohen_dcs/ADynamicComponentArchitectureForHighPerformanceGameplay.pptx) (Insomniac Games, 2010)
- [Translation and Analytical Commentary](https://hww.github.io/articles/2013/terrance_cohen_dcs/)

---

## ⚠️ Key Architectural Decisions

### 1. Inversion of the Original Structure

In the original DCS (for IBM Cell), a dense roster and sparse component array were used. In this implementation for modern CPUs, the structure is inverted: **dense component pools + sparse roster**. This ensures ideal data locality during iteration.

### 2. No Virtual Methods in Hot Paths

Type comparison is performed through a scalar `TypeId` (integer identifier), allowing the JIT compiler and Burst to optimize `switch` statements into jump tables.

### 3. Prius Initialization Pattern

Instead of passing heavy configuration data into components, the **Prius** pattern is used — an external object with initialization data passed to `Allocate()` and then to the component's `Init()` method.

### 4. Separation of Data and Event Contours

- **Data Contour**: State components (Health, Transform) — updated linearly, require maximum performance.
- **Event Contour**: Events and subscriptions — manage behavior, have a short lifecycle.

---

## 🧪 Example: Shot System

Instead of creating full GameObject instances for projectiles, a component-based approach is used:

```csharp
// Shot state component (long-lived)
public struct Shot : IComponent {
    public int RosterIndex { get; set; }
    public Handle CurrentAction;  // Reference to current action
}

// Action component (POD struct, can be processed asynchronously)
public struct ShotMoveForward : IComponent {
    public int RosterIndex { get; set; }
    public Vector3 Location;
    public Vector3 Direction;
    public float Speed;
}

// SPU (Job System) logic
public struct ShotMoveForwardJob : IJobParallelFor {
    public NativeArray<ShotMoveForward> Actions;
    public void Execute(int index) {
        Actions[index] = Actions[index] with {
            Location = Actions[index].Location + Actions[index].Direction * Actions[index].Speed * Time.deltaTime
        };
    }
}
```

---

## 🤝 Contributing

We welcome improvements and optimizations. Please review the documentation before submitting a Pull Request.

---

## 📄 License

MIT
