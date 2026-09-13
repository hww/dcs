# Interaction

**What can we do?**

`Interaction` describes actions that can be performed with objects and locations in the world.

It separates the physical existence of an object from the actions that object makes available.

---

## Purpose

Interaction answers:

> **What can an actor do here?**

Examples:

* open;
* use;
* pick up;
* hide;
* take cover;
* vault;
* climb;
* activate;
* operate.

---

## Responsibilities

Interaction owns:

* interactable objects;
* interaction points;
* interaction slots;
* available actions;
* interaction conditions;
* reservations;
* interaction runtime state.

---

## Smart Objects

An object may expose several independent interaction opportunities.

For example:

```text
Table
 ├── CoverSlot
 ├── VaultSlot
 └── InteractionSlot
```

Another object may have none.

The visual appearance of an object does not determine its interaction capabilities.

---

## Spatial Relationship

Interaction uses Spatial information to locate interaction opportunities.

```text
Spatial
   │
   │ where?
   ▼
Interaction
   │
   │ what can be done?
   ▼
Gameplay
   │
   │ what happens?
```

Spatial should not contain the interaction behavior itself.

---

## Interaction vs Navigation

These systems are related but different.

Navigation answers:

> Can the agent move there?

Interaction answers:

> Is there an action available there?

A vault is a good example where both systems cooperate:

```text
Interaction
  → identifies a VaultSlot

Navigation
  → determines traversal

Gameplay
  → determines what happens
```

---

## Does Not Own

Interaction does not own:

* geometry algorithms;
* spatial indexing;
* pathfinding;
* gameplay rules;
* world authoring.

An interaction point can be authored in World, represented spatially by Spatial, used by Navigation, and executed through Gameplay.

---

## Reservations

Some interactions cannot be used by multiple actors simultaneously.

Interaction may therefore own reservation state:

```text
Available
   ↓
Reserved
   ↓
Occupied
   ↓
Available
```

This is especially useful for:

* cover;
* seats;
* weapon stations;
* doors;
* special interaction points.

---

## Mental Model

Think of Interaction as the **action layer attached to the world**.

> Spatial tells us where something is. Interaction tells us what an actor can do there.

---

## Design Principle

> **An object existing in the world does not imply that it is interactable.**
