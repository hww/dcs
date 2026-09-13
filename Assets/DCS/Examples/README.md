# Examples

**Unity scenes for testing and demonstration.**

`Examples` shows how DCS systems are intended to be used.

Examples are executable documentation.

---

## Purpose

Examples answer:

> **How do I use this system?**

---

## Responsibilities

Examples may demonstrate:

* Core components;
* events;
* facts;
* Lua;
* world authoring;
* spatial queries;
* navigation;
* interaction;
* gameplay integration.

---

## Example vs Test

An Example demonstrates intended usage.

A Test verifies behavior.

```text
Example
  → "This is how you use it."

Test
  → "This is what must remain true."
```

---

## Rules

Examples should:

* be small;
* demonstrate one concept where possible;
* avoid production-only hacks;
* remain easy to run;
* use public APIs;
* document unusual setup requirements.

---

## Does Not Own

Examples should not contain production implementations.

If a feature is needed by an example, the feature belongs in its actual subsystem.

---

## Mental Model

Examples are **executable documentation**.

A developer should be able to open an example and understand how the corresponding DCS API is intended to be used.

---

## Design Principle

> **If an API is difficult to demonstrate, the API may need improvement.**
