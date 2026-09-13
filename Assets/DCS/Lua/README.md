# Lua

**How is gameplay scripted?**

Lua provides the higher-level scripting layer of DCS.

It allows gameplay logic and designer-facing behavior to interact with the runtime without exposing internal C# implementation details.

---

## Purpose

Lua answers:

> **How does high-level gameplay communicate with the runtime?**

---

## Responsibilities

Lua owns:

* Lua runtime;
* Lua state;
* script contexts;
* Lua bindings;
* C# ↔ Lua communication;
* gameplay scripting APIs;
* event bindings;
* world bindings;
* spatial bindings;
* component bindings.

---

## Architecture

The relationship between Lua and the runtime is intentionally indirect.

```text
Lua Script
    │
    ▼
Lua Binding
    │
    ▼
DCS API
    │
    ▼
Runtime System
```

Lua should consume contracts rather than internal data structures.

---

## C# and Lua

DCS uses two complementary levels:

```text
C#
 └── performance-critical runtime

Lua
 └── high-level gameplay and scripting
```

C# should handle work where predictable performance and low-level control are important.

Lua should provide flexibility for higher-level gameplay behavior.

---

## Bindings

Bindings should be organized by domain.

For example:

```text
Lua
 ├── Core
 └── Bindings
      ├── ECS
      ├── Events
      ├── Map
      ├── Spatial
      └── Zone
```

Bindings are an API boundary.

They should not become a second implementation of the underlying system.

---

## Does Not Own

Lua does not own:

* component storage;
* spatial indexes;
* world datasets;
* navigation structures;
* physics;
* gameplay state that belongs to C# runtime systems.

Lua requests or reacts through APIs provided by those systems.

---

## Mental Model

Lua is the **scripting interface to DCS**, not the runtime itself.

> Lua describes high-level behavior. DCS provides the mechanisms that execute it.

---

## Design Principle

> **Expose stable concepts, not implementation details.**
