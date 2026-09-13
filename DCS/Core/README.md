# Core

**Dynamic Component System — the infrastructure everything runs on.**

`Core` is the foundation of DCS.

It provides the runtime mechanisms required by other systems without knowing what the actual game is about.

DCS is intentionally built on top of Core rather than putting gameplay-specific logic into the foundation.

---

## Purpose

Core answers:

> **How does the system work?**

It provides the common runtime infrastructure for:

* dynamic components;
* hosts and identity;
* component storage;
* events;
* facts;
* scheduling;
* handlers;
* reflection and generated metadata.

---

## Responsibilities

Core owns the mechanisms that are shared by many independent systems.

Typical responsibilities:

```text
Core
 ├── Components
 ├── Hosts
 ├── Facts
 ├── Events
 ├── Scheduling
 ├── Handlers
 └── Reflection
```

These systems should be usable by World, Spatial, Gameplay, Navigation, Interaction and Lua without Core knowing about those domains.

---

## Does Not Own

Core must not become a container for everything that is "important".

It does **not** own:

* Zones;
* Actors;
* Navigation;
* Spatial geometry;
* Interaction logic;
* quests;
* combat;
* AI;
* world authoring;
* game-specific rules.

For example:

```text
Bad:

Core
 └── ZoneManager
```

Instead:

```text
Gameplay
 └── Zones
      └── ZoneRuntime
```

which uses Core infrastructure where necessary.

---

## Dependency Rule

Core should have the smallest possible knowledge of the rest of DCS.

```text
World ──────────┐
Spatial ────────┤
Gameplay ───────┤
Navigation ─────┤──► Core
Interaction ────┤
Lua ────────────┘
```

The reverse dependency should generally not exist.

```text
Core
  ✗ Gameplay
  ✗ World
  ✗ Navigation
  ✗ Interaction
```

---

## Mental Model

Think of Core as the **operating machinery** of DCS.

It does not decide:

> "What should happen?"

It provides the mechanisms that allow another system to decide that.

For example:

```text
Gameplay
   │
   ├── decides what should happen
   │
   ▼
Core Event System
   │
   └── delivers the event
```

Core provides the mechanism.

The domain system provides the meaning.

---

## Why Core Is Abstract

DCS is designed to support different kinds of gameplay systems.

Therefore Core cannot assume that a component represents:

* an enemy;
* a door;
* a weapon;
* a zone;
* a navigation agent.

Instead it works with generic concepts such as:

```text
Host
Component
Event
Fact
Handler
Schedule
```

This is one of the main sources of DCS's flexibility — and also one of the reasons the architecture requires explicit documentation.

---

## Structure

The exact folder structure may evolve, but conceptually:

```text
Core/
 ├── Runtime/
 ├── Components/
 ├── Host/
 ├── Facts/
 ├── Events/
 ├── Scheduling/
 └── Reflection/
```

Each subsystem should remain focused on its own mechanism.

---

## Design Principle

> **Core provides mechanisms, not meaning.**

If a feature can be described without knowing anything about the game, it may belong in Core.

If it requires game-specific concepts, it probably belongs somewhere above Core.
