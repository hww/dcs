# Dynamic Component System (DCS)

A dynamic component framework for Unity that supports data-oriented runtime systems,
world metadata, spatial queries and Lua scripting.

## Background

DCS started as a personal experiment: could a data-oriented system be built in Unity,
on C#, with Lua as a thin scripting layer?

The full story — including the design decisions, the trade-offs and the honest
conclusions — is in the experiment report:

**[RU] Experiment report (in Russian)**
[Read article](https://hww.github.io/articles/2026/dcs_unity/)

---

## Core Philosophy

### Universalize contracts, not implementations

Different systems may use different internal data structures while exposing stable
contracts to their consumers.

### Separate data from behavior

Compiled world data should be optimized for consumption by runtime systems.

### Keep domain boundaries explicit

A class should have one obvious architectural home.

### Avoid universal managers

Do not create a manager simply because several systems need to communicate.
Prefer small systems with explicit contracts.

### Build expensive information offline

If information can be compiled once, do not reconstruct it every frame or every load.

### Optimize the hot path without hiding the architecture

Performance-oriented storage belongs inside the appropriate subsystem. The public
architecture should remain understandable.

---

## Architecture at a Glance

Every subsystem in DCS answers **one** question. This is not a stylistic choice —
it is how the framework keeps boundaries explicit.

```text
Core
  "How does everything work?"

World
  "What is everything made of?"

Spatial
  "Where is everything?"

Gameplay
  "What is happening?"

Navigation
  "Where can we go?"

Interaction
  "What can we do?"

Lua
  "How is high-level gameplay scripted?"
```

The main relationship is:

```text
                         ┌──────────────┐
                         │     Core     │
                         │  How it works│
                         └──────┬───────┘
                                │
              ┌─────────────────┼─────────────────┐
              ▼                 ▼                 ▼
           World             Spatial             Lua
        "What exists?"      "Where?"          "How script?"
              │                 │
              │          ┌──────┴──────┐
              │          ▼             ▼
              │     Navigation    Interaction
              │       "Where?"      "What?"
              │          \             /
              └───────────\───────────/
                           ▼
                       Gameplay
                    "What happens?"
```

---

## Project Manifest

Main DCS Manifest [README](DCS/README.md)

| Module | Question | Responsibility | Documentation |
| --- | --- | --- | --- |
| **Core** | How does everything work? | Dynamic components, hosts, handles, facts, events and scheduling | [README](DCS/Core/README.md) · [Reference](DCS/Docs/DCS_Core_Reference.md) |
| **Spatial** | Where is everything? | Geometry, surfaces, spatial index and spatial queries | [README](DCS/Spatial/README.md) |
| **Gameplay** | What is happening? | Gameplay rules, state, zones and runtime reactions | [README](DCS/Gameplay/README.md) |
| **Navigation** | Where can we go? | Navigation surfaces, paths, costs and traversal | [README](Navigation/README.md) |
| **Interaction** | What can we do? | Interactable objects, slots, actions and reservations | [README](DCS/Interaction/README.md) |
| **Lua** | How is gameplay scripted? | Lua runtime and C# ↔ Lua bindings | [README](DCS/Lua/README.md) |
| **Libs** | What makes code easier to write? | Small reusable helpers and syntax sugar | [README](DCS/Libs/README.md) |
| **Plugins** | What external code do we use? | DLLs, native libraries and third-party dependencies | [README](DCS/Plugins/README.md) |
| **Examples** | How do we use DCS? | Demonstration and experimental Unity scenes | [README](DCS/Examples/README.md) |
| **Tests** | Does it work? | Unit, integration, build and runtime test benches | [README](DCS/Tests/README.md) |

---

## Documentation

DCS uses two documentation levels.

**Module READMEs.** Every major module has a short `README.md`. These describe
purpose, responsibilities, what the module does *not* own, dependency direction,
mental model and internal structure. They are architectural maps, not API references.

**Detailed documentation.** Long-form technical documentation lives in `DCS/Docs/`.
The existing `dcs-documentation.md` describes the **Core DCS implementation** —
hosts, handles, component pools, event systems, chains, registries and scheduling —
and should be renamed to:

```text
Docs/DCS_Core_Reference.md
```

This keeps the name aligned with what the document actually describes.

---

## Architectural Boundaries

DCS deliberately separates concepts that are often mixed together.

- **World** — what exists, how it is authored, compiled and streamed.
- **Spatial** — where things are and what spatial metadata is associated with them.
- **Navigation** — where an agent can move and how it can traverse the world.
- **Interaction** — what actions are available at an object or location.
- **Gameplay** — what those facts mean for the game and what happens as a consequence.

For example:

```text
A table exists
      │
      ▼
World
"this object exists"
      │
      ▼
Spatial
"this surface is here"
      │
      ├──────────────► Navigation
      │                "this agent can traverse it"
      │
      └──────────────► Interaction
                       "this object has a VaultSlot"
                                │
                                ▼
                            Gameplay
                       "perform the vault"
```

No single system needs to own all of these meanings.

---

## Data Flow

Authoring data and runtime data are intentionally separated.

```text
Unity Authoring
      │
      ▼
Build / Compile
      │
      ▼
Compiled Data
      │
      ├── Spatial
      ├── Navigation
      ├── Interaction
      └── Gameplay
      │
      ▼
Runtime
```

The goal is to move expensive or repetitive work out of runtime whenever it can be
prepared during the build phase.

---

## Repository Structure

```text
Assets/DCS/
│
├── Core/             → How does everything work?
├── World/            → What is everything made of?
├── Spatial/          → Where is everything?
├── Gameplay/         → What is happening?
├── Navigation/       → Where can we go?
├── Interaction/      → What can we do?
├── Lua/              → How is gameplay scripted?
├── Libs/             → What makes implementation easier?
├── Plugins/          → What external code do we use?
├── Examples/         → How do we use DCS?
├── Tests/            → Does it work?
└── Docs/             → Detailed technical documentation
```

---

## A Rule for Adding New Code

When adding a new feature, ask:

```text
Where is it?
        → Spatial

What is it made of / where is it authored?
        → World

Where can it go?
        → Navigation

What can be done with it?
        → Interaction

What happens because of it?
        → Gameplay

Is it generic infrastructure?
        → Core
```

If the answer is unclear, do not immediately create another `Manager`.
The ambiguity usually means that the responsibility needs to be clarified first.

---

## Status

DCS is an evolving framework.

The module READMEs define the intended architectural boundaries.
Detailed documentation describes the current implementation state.
```
