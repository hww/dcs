# Gameplay

**What is happening?**

`Gameplay` contains the game-specific behavior and rules that turn world state into gameplay.

It is where spatial facts, player actions, events and game state become meaningful behavior.

---

## Purpose

Gameplay answers:

> **What is happening?**

Examples:

* A player entered a gameplay zone.
* An encounter started.
* A trigger activated.
* A gameplay state changed.
* An event caused another system to react.

---

## Responsibilities

Gameplay owns:

* gameplay rules;
* gameplay state;
* gameplay reactions;
* gameplay zones;
* zone activation;
* gameplay events;
* game-specific behavior.

---

## Zones

A Zone is a **gameplay concept**, not a geometry system.

A zone means:

> **This area has gameplay meaning.**

The actual shape of that area can be represented by Spatial.

```text
Gameplay Zone
      │
      └── references
              │
              ▼
           Spatial
```

This prevents Zone from becoming a general-purpose collision, navigation or geometry framework.

---

## Does Not Own

Gameplay does not own:

* geometric intersection algorithms;
* spatial indexes;
* pathfinding;
* physical collision;
* generic interaction slots.

For example:

```text
Gameplay
  ✓ "Player entered CombatArea"

Spatial
  ✓ "Player is inside this geometry"

Navigation
  ✓ "Agent can traverse this surface"

Interaction
  ✓ "This table has a VaultSlot"
```

---

## Events

Gameplay can react to events produced by other systems.

For example:

```text
Spatial
   │
   │ Spatial event
   ▼
Gameplay
   │
   │ gameplay decision
   ▼
Core Event System
```

The spatial system should not need to know what gameplay reaction will happen.

---

## Lua

Gameplay is one of the primary consumers of the Lua scripting layer.

Lua should interact with Gameplay through stable APIs rather than internal implementation details.

---

## Mental Model

Think of Gameplay as the **meaning and consequences layer**.

Spatial can say:

> "The player is inside volume X."

Gameplay can say:

> "Volume X is a combat encounter area, so start the encounter."

---

## Design Principle

> **Gameplay owns meaning and consequences, not geometry.**

If a class exists primarily to determine geometric relationships, it probably does not belong here.
