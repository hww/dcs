# World

**The world runtime and authoring — what is everything made of?**

`World` is the bridge between Unity-authored content and the runtime representation of the game world.

It describes what exists in the world, how designers author it, how that data is compiled, and how it is loaded at runtime.

---

## Purpose

World answers:

> **What is the world made of?**

It owns the lifecycle of world data:

```text
Authoring
    ↓
Build
    ↓
Runtime Data
    ↓
Streaming
```

---

## Responsibilities

World owns:

* Unity authoring objects;
* world entities;
* actors;
* locators;
* spawners;
* gameplay zone authoring;
* compiled world datasets;
* world data records;
* build and baking;
* dataset loading and unloading;
* world streaming.

---

## Authoring Is Not Runtime

Unity components are primarily an authoring interface.

They are convenient for designers because they provide:

* Inspector editing;
* Scene view visualization;
* handles;
* Undo;
* Unity serialization.

Runtime systems should not depend on the authoring representation when it can be avoided.

Instead:

```text
Unity Scene
     │
     ▼
Authoring Components
     │
     ▼
World Build
     │
     ▼
Compiled Dataset
     │
     ▼
Runtime Systems
```

---

## Does Not Own

World does not decide:

* how spatial queries work;
* how navigation works;
* what an interaction does;
* what gameplay rules mean.

For example, World may contain a table as authored content.

It does not decide whether that table is:

* walkable;
* usable as cover;
* vaultable;
* interactable.

Those meanings belong to Spatial, Navigation, Interaction and Gameplay.

---

## Data Ownership

World owns the **compiled representation of world content**.

Other systems may consume parts of this data.

```text
World Dataset
    │
    ├── Spatial data
    ├── Navigation data
    ├── Interaction data
    └── Gameplay data
```

The dataset is therefore a compiled boundary between authoring and runtime.

---

## Build

The Build layer converts designer-friendly authoring data into runtime-oriented data.

Build should:

* validate authoring data;
* compile geometry;
* compile metadata;
* generate runtime records;
* build references;
* prepare data for streaming.

Build code should not become runtime gameplay code.

---

## Streaming

World Streaming owns the lifecycle of world datasets:

```text
Unloaded
   ↓
Loading
   ↓
Loaded
   ↓
Unloading
   ↓
Unloaded
```

Streaming should coordinate loading and unloading.

It should not contain gameplay logic.

---

## Mental Model

Think of World as the **source and lifecycle of the world**.

> **World defines what exists. Other systems decide what that world means and how it is used.**

---

## Design Principle

> **Author once, compile once, consume many times.**

The runtime should not repeatedly reconstruct expensive information that could have been prepared during the build phase.
