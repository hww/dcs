# Navigation

**Where can we go?**

`Navigation` describes how agents move through the world.

It transforms world and spatial information into movement-oriented representations and queries.

---

## Purpose

Navigation answers:

> **Where can an agent go?**

And:

> **How can the agent get there?**

---

## Responsibilities

Navigation owns:

* navigation surfaces;
* navigation meshes or graphs;
* traversal costs;
* agent capabilities;
* path queries;
* traversal links;
* movement topology;
* jump / climb / vault / drop connections.

---

## Spatial Relationship

Navigation consumes Spatial information.

```text
World
  │
  ▼
Spatial
  │
  ▼
Navigation Build
  │
  ├── Navigation Mesh
  ├── Costs
  └── Traversal Links
```

Spatial describes the world.

Navigation builds a representation suitable for movement.

---

## Agent Capabilities

A surface should not necessarily contain absolute gameplay decisions such as:

```text
CanWalk = true
```

Instead, navigation evaluates world properties against an agent.

For example:

```text
Surface
 └── TraversalFlags

Agent
 └── Capabilities

Navigation
 └── CanTraverse(Surface, Agent)
```

This allows different agents to use the same world differently.

---

## Traversal

Not every movement connection can be represented as ordinary walkable surface.

Examples:

* jump;
* climb;
* vault;
* drop;
* ladder;
* door;
* special traversal.

These are represented separately as traversal links.

```text
Surface A
    │
    │ Traversal Link
    ▼
Surface B
```

---

## Does Not Own

Navigation does not own:

* gameplay rules;
* interaction execution;
* physical simulation;
* world authoring;
* generic spatial indexing.

A vault point may be discovered or described through Interaction, while Navigation determines whether movement between surfaces is possible.

---

## Runtime vs Build

Navigation may contain both:

```text
Build
  → generate navigation data

Runtime
  → query and use navigation data
```

Build-time work should not be repeated unnecessarily during gameplay.

---

## Mental Model

Think of Navigation as the **movement model of the world**.

> Spatial tells us what space exists. Navigation tells us how an agent can move through that space.

---

## Design Principle

> **Navigation describes movement possibilities, not gameplay intentions.**
