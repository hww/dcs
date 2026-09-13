# DCS

**Dynamic Component System**

DCS is a runtime and authoring framework for building gameplay systems in Unity.

The project is organized around a simple separation of concerns:

> **Core** makes things work.
> **Spatial** describes where things are.
> **World** defines what the world is made of.
> **Gameplay** defines what happens.
> **Navigation** defines where things can go.
> **Interaction** defines what can be done.

---

## Project Manifest

| Module          | Question                            | Responsibility                                                                  |
| --------------- | ----------------------------------- | ------------------------------------------------------------------------------- |
| **Core**        | **On what does everything work?**   | Dynamic components, hosts, facts, events, scheduling and runtime infrastructure |
| **World**       | **What is everything made of?**     | World authoring, runtime entities, datasets, compilation and streaming          |
| **Spatial**     | **Where is everything?**            | Game-world spatial metadata, geometry, surfaces and spatial queries             |
| **Gameplay**    | **What is happening?**              | Gameplay rules, state, zones and runtime reactions                              |
| **Navigation**  | **Where can we go?**                | Navigation data, movement topology, traversal and pathfinding                   |
| **Interaction** | **What can we do?**                 | Interactable objects, interaction points, actions and reservations              |
| **Lua**         | **How is gameplay scripted?**       | Lua runtime and bindings between scripts and DCS                                |
| **Libs**        | **What makes writing code easier?** | Small reusable helpers and syntax sugar                                         |
| **Plugins**     | **What do we depend on?**           | Third-party libraries, DLLs and external integrations                           |
| **Examples**    | **How do we use it?**               | Demonstration and experimental Unity scenes                                     |
| **Tests**       | **Does it work?**                   | Automated tests and isolated test benches                                       |

---

# Architecture

DCS deliberately separates several concepts that are often mixed together in game projects.

```text
                           ┌──────────────┐
                           │     Core     │
                           │ How it works │
                           └───────┬──────┘
                                   │
                    ┌──────────────┼──────────────┐
                    │              │              │
                    ▼              ▼              ▼
              ┌──────────┐   ┌──────────┐   ┌────────────┐
              │  World   │   │ Spatial  │   │    Lua     │
              │ What is  │   │ Where is │   │ Scriptable │
              └────┬─────┘   └────┬─────┘   └────────────┘
                   │              │
                   │        ┌─────┴─────┐
                   │        │           │
                   │        ▼           ▼
                   │  ┌──────────┐ ┌─────────────┐
                   │  │Navigation│ │ Interaction │
                   │  │Where?    │ │ What can?   │
                   │  └────┬─────┘ └──────┬──────┘
                   │       │              │
                   └───────┴──────┬───────┘
                                   ▼
                             ┌──────────┐
                             │ Gameplay │
                             │What happens│
                             └──────────┘
```

The important distinction is:

* **World** owns the description and construction of the world.
* **Spatial** provides spatial knowledge about that world.
* **Navigation** uses spatial information to answer movement questions.
* **Interaction** uses spatial information to answer interaction questions.
* **Gameplay** decides what should happen as a consequence.
* **Core** provides the infrastructure on which these systems run.

---

# Core

**Dynamic Component System → on what does everything work?**

`Core` contains the fundamental runtime infrastructure of DCS.

It should know as little as possible about the actual game.

Typical responsibilities:

* Dynamic Components
* Hosts and Host identity
* Component storage
* Component registration
* Events
* Event subscriptions
* Facts
* Scheduling
* Runtime handlers
* Reflection / generated metadata
* Core interfaces and attributes

```text
Core
 ├── Runtime
 ├── Components
 ├── Host
 ├── Facts
 ├── Events
 ├── Scheduling
 └── Reflection
```

### Principle

> **Core provides mechanisms, not gameplay.**

A new gameplay feature should normally depend on Core rather than modify Core.

---

# World

**The world runtime and authoring → what is everything made of?**

`World` describes the physical and authored composition of the game world.

It connects Unity authoring with runtime data.

Responsibilities:

* Unity authoring objects
* Actors
* Locators
* Spawners
* Gameplay zone authoring
* World datasets
* Runtime world entities
* Build / baking
* Data compilation
* World streaming

```text
World
 ├── Authoring
 ├── Data
 ├── Build
 ├── Runtime
 └── Streaming
```

The important distinction:

> **Authoring data is not runtime data.**

Unity components are convenient for designers.

Runtime datasets should be compiled into representations optimized for the systems that consume them.

```text
Unity Scene
    │
    ▼
Authoring
    │
    ▼
Build / Compile
    │
    ▼
Runtime Dataset
    │
    ├── Spatial
    ├── Navigation
    ├── Interaction
    └── Gameplay
```

---

# Spatial

**Game metadata system → where?**

`Spatial` answers questions about the world in space.

It is not a Zone system.

It is not Unity Physics.

It is not Navigation.

It is the spatial information layer shared by other systems.

Responsibilities include:

* Geometry
* Spatial metadata
* Surfaces
* Spatial indexing
* Spatial queries
* Raycasts
* Point queries
* Radius queries
* Nearest-object queries
* Geometry intersection
* Spatial hits
* Surface semantics

Example:

```text
Raycast
    │
    ▼
Spatial
    │
    ├── Position
    ├── Normal
    ├── Object
    ├── Surface
    └── Metadata
```

A spatial query should be able to answer:

> "What did I hit, where did I hit it, what surface is it, and what does that surface mean?"

### Principle

> **Spatial knows where. It does not decide what gameplay should happen.**

---

# Gameplay

**What is happening?**

`Gameplay` contains game-specific behavior and rules.

Examples:

* Gameplay zones
* Zone activation
* Gameplay state
* Runtime reactions
* Rules
* Game events
* State transitions

A `Zone` belongs conceptually to Gameplay because its meaning is:

> "Something happens when an entity enters this region."

The shape describing that region is a spatial concern.

Therefore:

```text
Gameplay Zone
      │
      └── references spatial representation
```

rather than:

```text
Spatial
   └── Zone
       └── Gameplay
```

### Principle

> **Geometry describes space. Gameplay gives that space meaning.**

---

# Navigation

**Where can we go?**

`Navigation` describes movement through the world.

Responsibilities:

* Navigation surfaces
* Navigation mesh / graph
* Navigation costs
* Agent capabilities
* Traversal
* Links
* Jump / climb / vault / drop connections
* Path queries

Navigation consumes information from Spatial and World, but remains a separate system.

```text
World Geometry
      │
      ▼
   Spatial
      │
      ▼
 Navigation Build
      │
      ├── NavMesh
      ├── Costs
      └── Traversal Links
```

A surface should not necessarily say:

```text
CanWalk = true
```

Instead, the surface describes itself and the navigation system evaluates it against an agent's capabilities.

For example:

```text
Surface
 ├── Material
 ├── Slope
 ├── Height
 ├── TraversalFlags
 └── Metadata
```

An agent then determines whether it can use it.

---

# Interaction

**What can we do?**

`Interaction` describes actions that can be performed in the world.

Examples:

* Doors
* Cover positions
* Vault points
* Climb points
* Pickup locations
* Interaction slots
* Smart Objects
* Reservations
* Interaction availability

A physical object and an interaction are not necessarily the same thing.

For example, a table can provide:

```text
Table
 ├── CoverSlot
 ├── VaultSlot
 └── InteractionSlot
```

while another visually similar object may provide none of them.

### Principle

> **Geometry describes the object. Interaction describes what can be done with it.**

---

# Lua

**How is gameplay scripted?**

Lua provides the higher-level scripting layer of DCS.

Responsibilities:

* Lua runtime
* Lua state
* Script contexts
* Lua ↔ C# bindings
* Gameplay scripting API
* Event bindings
* Spatial bindings
* World bindings
* ECS / component bindings

The intended relationship is:

```text
Lua
 │
 │ request / subscribe / poll
 ▼
DCS Runtime
 │
 ▼
Core / Gameplay / Spatial / ...
```

Lua should consume stable system APIs rather than directly manipulating internal runtime structures.

---

# Libs

**Helpers with syntax sugar**

`Libs` contains small reusable utilities that make the rest of the code easier to write.

Examples:

* Utility extensions
* Small helpers
* Convenience APIs
* Reusable algorithms
* Syntax sugar

`Libs` should remain lightweight.

A helper belongs here when it is genuinely reusable and does not represent a domain system.

---

# Plugins

**DLLs and other 3rd-party libraries**

`Plugins` contains external dependencies required by DCS.

Examples:

* DLLs
* Third-party libraries
* Native plugins
* External integrations
* Source generators

DCS-specific architecture should not be hidden inside `Plugins`.

Plugins are dependencies, not DCS domains.

---

# Examples

**The Unity scenes for testing and demonstration**

`Examples` contains practical examples of using DCS.

Examples are intentionally separate from production code.

They can demonstrate:

* Core components
* Events
* Lua
* World authoring
* Spatial queries
* Navigation
* Interaction
* Integration between systems

An example should answer:

> "How do I use this system?"

rather than:

> "How is this system implemented?"

---

# Tests

**Test benches**

`Tests` contains automated tests and isolated environments for validating DCS.

Tests should mirror the architecture:

```text
Tests
 ├── EditMode
 │    ├── Core
 │    ├── Spatial
 │    ├── World
 │    ├── Navigation
 │    ├── Interaction
 │    └── Gameplay
 │
 └── PlayMode
      ├── Core
      ├── Spatial
      ├── World
      ├── Navigation
      ├── Interaction
      └── Gameplay
```

Tests are not examples.

An **Example** demonstrates intended usage.

A **Test** proves behavior.

---

# Dependency Rules

The architecture should remain understandable as DCS grows.

Preferred dependency direction:

```text
                    Core
                     ▲
                     │
        ┌────────────┼────────────┐
        │            │            │
      World       Spatial       Lua
        │            ▲
        │            │
        ├───────► Navigation
        │            │
        └───────► Interaction
                     ▲
                     │
                  Gameplay
```

More importantly:

### Core does not depend on gameplay

```text
Core
  ✗ Zone
  ✗ Enemy
  ✗ Weapon
  ✗ Navigation
```

### Spatial does not contain gameplay rules

```text
Spatial
  ✓ Geometry
  ✓ Surface
  ✓ Spatial queries

Spatial
  ✗ Zone activation
  ✗ Quest logic
  ✗ Combat rules
```

### Navigation does not own world authoring

```text
World → builds data
Navigation → consumes data
```

### Gameplay does not implement geometry algorithms

```text
Gameplay → asks Spatial
Spatial  → answers spatial questions
```

---

# The Mental Model

When adding a new feature, ask four questions.

### 1. Where is it?

Put spatial representation in **Spatial**.

```text
Where is the object?
What did the ray hit?
What surface is here?
What is nearby?
```

### 2. What is it made of?

Put authoring, world composition and compiled world data in **World**.

```text
What exists in the scene?
How is it authored?
How is it compiled?
How is it streamed?
```

### 3. What can be done with it?

Put capabilities and available actions in **Interaction**.

```text
Can I vault this?
Can I take cover here?
Can I open this?
Can I interact with this?
```

### 4. What happens?

Put rules and consequences in **Gameplay**.

```text
What happens when I enter?
What happens when I interact?
What state changes?
What event is produced?
```

---

# Core Philosophy

DCS follows one central rule:

> **Do not make one system responsible for answering every question.**

Instead, build specialized systems with clear contracts.

```text
                 WORLD
           "What exists?"
                  │
                  ▼
               SPATIAL
             "Where is it?"
              /         \
             ▼           ▼
       NAVIGATION    INTERACTION
       "Where can?"  "What can?"
             \           /
              ▼         ▼
                 GAMEPLAY
              "What happens?"
```

The systems share data and infrastructure where useful, but they do not collapse into a single universal manager.

---

# Folder Overview

```text
DCS/
│
├── Core/           → On what does everything work?
├── World/          → What is everything made of?
├── Spatial/        → Where is everything?
├── Gameplay/       → What is happening?
├── Navigation/     → Where can we go?
├── Interaction/    → What can we do?
├── Lua/            → How is gameplay scripted?
├── Libs/           → What makes code easier to write?
├── Plugins/        → What external code do we use?
├── Examples/       → How do we use DCS?
└── Tests/          → Does it work?
```

This structure is intentionally organized around **responsibilities and questions**, rather than around individual Unity component types.

That makes the architecture easier to understand as new systems are added.
