# Libs

**Helpers with syntax sugar.**

`Libs` contains small, reusable utilities that simplify implementation across DCS.

---

## Purpose

Libs answers:

> **Can this common piece of code be made simpler without becoming another system?**

---

## Responsibilities

Libs may contain:

* extension methods;
* utility functions;
* small reusable algorithms;
* convenience wrappers;
* syntax sugar;
* generic helpers.

---

## Does Not Own

Libs should not contain domain systems.

Avoid putting things such as:

```text
ZoneManager
SpatialManager
NavigationManager
GameplayManager
```

into Libs.

If something has domain-specific state, lifecycle or rules, it belongs in the appropriate subsystem.

---

## Dependency Rule

Libs should remain as independent as possible.

A useful helper should not require the entire DCS architecture merely to be used.

---

## Mental Model

Think of Libs as a **toolbox**, not a subsystem.

---

## Design Principle

> **Small, boring and reusable is good.**

If a helper starts accumulating state, configuration, lifecycle and domain knowledge, it probably wants to become a real subsystem.
