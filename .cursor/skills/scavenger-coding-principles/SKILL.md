---
name: scavenger-coding-principles
description: >-
  Scavenger-wide coding principles, common pitfalls, and implementation habits (DRY,
  parameterization, readability). Use when writing or refactoring C# or UI code, adding
  behavior similar to existing code, choosing between duplicating vs extending a helper,
  or when the user asks for quality habits or code review–style guidance. For repo layout,
  formatting policy, and where files belong, read scavenger-conventions.
---

# Scavenger — coding principles

**How** to implement: judgment calls while you write. **Where** files go and **what** the repo expects structurally is **[`scavenger-conventions`](../scavenger-conventions/SKILL.md)**. **System boundaries and flows** are **[`scavenger-architecture`](../scavenger-architecture/SKILL.md)** (and its [`reference.md`](../scavenger-architecture/reference.md)). After edits, still follow **[`scavenger-post-change-verification`](../scavenger-post-change-verification/SKILL.md)** for self-review, console, and tests—do not duplicate that checklist here.

## How to extend this skill

Add new **`##` sections** with short bullets. Prefer links to `reference.md` in this folder only when examples would bloat this file. Keep each new topic **actionable** (when to apply, what to do, what to avoid).

## Principle: one implementation over copy-paste (DRY)

- **Before** writing a second function that mirrors an existing one (same loop, same walk, same validation with one branch different): **search** the codebase for the same pattern. Prefer **extending** the existing function with a parameter, optional flag, or small strategy hook—or a **thin wrapper** that calls a shared core.
- **Parameterization:** optional `bool` / enum for toggles; `Func<>` or a small interface only when branches multiply or tests need injection—avoid over-abstraction.
- **Red flags:** pasting a whole method and editing a few lines; two files with parallel structure; comments like “same as X but …” without a shared implementation.
- **Exception:** a deliberate fork is fine if the user asks for it for review, or if merging would risk large unrelated churn—say so briefly.

## Error handling and invariants

*Reserved.* Add bullets here as the team agrees (e.g. fail-fast vs defensive guards, null contracts, Unity-specific null checks).

## Unity / C# pitfalls

*Reserved.* Add bullets here (e.g. `Destroy` vs inactive objects, serialization rules, LINQ in hot paths)—keep each entry short and link to code or issues when useful.

## Related

- Repo layout and naming: [`scavenger-conventions`](../scavenger-conventions/SKILL.md)
- Architecture map: [`scavenger-architecture`](../scavenger-architecture/SKILL.md)
- Post-change verification: [`scavenger-post-change-verification`](../scavenger-post-change-verification/SKILL.md)
