# Tests

**Test benches.**

`Tests` verifies that DCS systems behave correctly and continue to behave correctly as the architecture evolves.

---

## Purpose

Tests answer:

> **Does it work?**

And:

> **Does it still work after we change it?**

---

## Responsibilities

Tests cover:

* Core;
* World;
* Spatial;
* Gameplay;
* Navigation;
* Interaction;
* Lua;
* integration between systems.

---

## EditMode / PlayMode

Tests are separated according to what they require.

```text
EditMode
 └── tests that do not require a running Unity scene

PlayMode
 └── tests that require runtime Unity behavior
```

Prefer EditMode tests when a system can be tested without Unity runtime state.

---

## Architecture

Tests should mirror production architecture.

```text
Tests/
 ├── EditMode/
 │    ├── Core/
 │    ├── World/
 │    ├── Spatial/
 │    ├── Gameplay/
 │    ├── Navigation/
 │    └── Interaction/
 │
 └── PlayMode/
      ├── Core/
      ├── World/
      ├── Spatial/
      ├── Gameplay/
      ├── Navigation/
      └── Interaction/
```

---

## Test Types

Useful categories include:

### Unit tests

Test one piece of logic in isolation.

### Integration tests

Test communication between DCS systems.

### Runtime tests

Verify behavior inside an actual Unity runtime.

### Build tests

Verify that authoring data can be compiled into valid runtime data.

### Data tests

Verify compiled datasets and their invariants.

---

## Important Rule

Tests should test **contracts**, not implementation details.

For example, a Spatial test should verify:

```text
Raycast(...)
    → returns the correct hit
```

rather than depending on:

```text
Dictionary<Vector3Int, List<int>>
```

being the implementation.

This allows the internal architecture to evolve without rewriting every test.

---

## Mental Model

Tests are the **safety net for architecture**.

This is particularly important for DCS because the system contains many abstractions and multiple layers that depend on stable contracts.

---

## Design Principle

> **Every important abstraction should have a reason to exist — and important behavior should have a test proving its contract.**
