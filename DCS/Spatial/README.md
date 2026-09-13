# Spatial

**Game metadata system — where is everything?**

`Spatial` provides the spatial representation and query infrastructure used by other DCS systems.

It answers questions about the world in space without deciding what those results mean for gameplay.

---

## Purpose

Spatial answers:

> **Where is it?**

Examples:

* What is at this position?
* What is nearby?
* What did this ray hit?
* What surface did we hit?
* Which objects intersect this area?
* What is the nearest object matching a filter?

---

## Responsibilities

Spatial owns:

* geometric representation;
* spatial metadata;
* spatial surfaces;
* spatial indexing;
* broad-phase queries;
* narrow-phase intersection;
* raycasts;
* point queries;
* radius queries;
* nearest-object queries;
* spatial hit results.

---

## Geometry

Spatial geometry describes **where something exists**.

Possible runtime representations include:

```text
Geometry
 ├── Sphere
 ├── Box
 ├── Cylinder
 └── Triangle
```

The runtime representation does not have to match the authoring representation.

A designer may author a complex object while the runtime uses an optimized representation.

---

## Metadata

Geometry alone is not enough.

A spatial hit may need to provide:

```text
SpatialHit
 ├── Position
 ├── Normal
 ├── Distance
 ├── ObjectID
 ├── SurfaceID
 ├── GeometryID
 └── Metadata / Flags
```

This allows other systems to interpret the result without duplicating spatial logic.

---

## Surfaces

A surface describes properties associated with a piece of geometry.

For example:

```text
Surface
 ├── Type
 ├── Normal
 ├── Material
 ├── TraversalFlags
 └── Metadata
```

Spatial describes the surface.

Navigation or Gameplay decides what that information means.

---

## Queries

Spatial should expose stable query contracts rather than expose its internal storage.

Conceptually:

```text
Point
Raycast
Radius
Nearest
Overlap
```

The underlying implementation may change from:

```text
Grid
```

to:

```text
BVH
```

or another structure without changing consumers.

---

## Broad Phase / Narrow Phase

Spatial queries should generally use two stages:

```text
Query
  │
  ▼
Broad Phase
  │
  │ candidate objects
  ▼
Narrow Phase
  │
  │ exact geometry test
  ▼
Spatial Result
```

This keeps runtime queries efficient while preserving geometric accuracy.

---

## Does Not Own

Spatial does not own:

* gameplay rules;
* zone activation;
* pathfinding;
* interaction behavior;
* physics simulation;
* AI decisions.

For example:

> Spatial can tell us that a ray hit a table surface.

It should not decide:

> "The player has entered cover mode."

That is another system's responsibility.

---

## Physics Is Not Spatial

Unity Physics answers simulation-oriented questions.

Spatial exists for **game-world metadata and deterministic gameplay queries**.

The two systems may share source geometry, but they should not be forced into the same abstraction.

---

## Mental Model

Think of Spatial as the **map of meaning attached to physical space**.

> **Spatial tells you where something is and what spatial information is associated with it.**

---

## Design Principle

> **Universalize the query contract, not the storage.**

Different geometry types and specialized runtime structures are allowed underneath one consistent query API.
