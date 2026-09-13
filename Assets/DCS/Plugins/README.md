# Plugins

**DLLs and third-party libraries.**

`Plugins` contains external code required by DCS or the Unity project.

---

## Purpose

Plugins answer:

> **What external technology does DCS depend on?**

---

## Responsibilities

Plugins may contain:

* managed DLLs;
* native libraries;
* third-party frameworks;
* Unity packages distributed as plugins;
* source generators;
* external integrations.

---

## Does Not Own

DCS architecture should not be hidden inside Plugins.

A plugin is an external dependency.

DCS-specific adapters should normally live in the appropriate DCS module.

For example:

```text
Plugin
   │
   ▼
DCS Adapter
   │
   ▼
DCS System
```

rather than spreading third-party API usage throughout the entire project.

---

## Dependency Management

External dependencies should have a clear boundary.

When possible:

```text
DCS
 │
 └── Adapter
       │
       └── Third-party library
```

This makes replacing or upgrading dependencies easier.

---

## Mental Model

Plugins are **dependencies, not architecture**.

---

## Design Principle

> **Keep external technology at the edge of the system.**
