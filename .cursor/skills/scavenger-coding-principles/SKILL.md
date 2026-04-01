---
name: scavenger-coding-principles
description: >-
  Coding habits/pitfalls (DRY, parameterize, readability). Use while writing/refactoring
  C#/UI code or when choosing reuse vs copy-paste. Repo layout/style: `scavenger-conventions`.
---

# Scavenger — coding principles

Implementation habits while you write. Repo layout: **[`scavenger-conventions`](../scavenger-conventions/SKILL.md)**. System boundaries: **[`scavenger-architecture`](../scavenger-architecture/SKILL.md)**. After changes, follow **[`scavenger-post-change-verification`](../scavenger-post-change-verification/SKILL.md)**.

## How to extend this skill

Add new **`##` sections** with short bullets. Prefer links to `reference.md` in this folder only when examples would bloat this file. Keep each new topic **actionable** (when to apply, what to do, what to avoid).

## Principle: one implementation over copy-paste (DRY)

- **Before** writing a second function that mirrors an existing one (same loop, same walk, same validation with one branch different): **search** the codebase for the same pattern. Prefer **extending** the existing function with a parameter, optional flag, or small strategy hook—or a **thin wrapper** that calls a shared core.
- **Parameterization:** optional `bool` / enum for toggles; `Func<>` or a small interface only when branches multiply or tests need injection—avoid over-abstraction.
- **Red flags:** pasting a whole method and editing a few lines; two files with parallel structure; comments like “same as X but …” without a shared implementation.
- **Exception:** a deliberate fork is fine if the user asks for it for review, or if merging would risk large unrelated churn—say so briefly.

## Error handling and invariants

- **Fail fast:** invalid state, broken contracts, or “this should never happen” paths should **surface immediately**—`Debug.Assert`, `UnityEngine.Assertions.Assert`, explicit `throw`, or **`Debug.LogError` / `LogError`** with enough context to fix. Do **not** swallow exceptions, return default values in response to garbage input, or continue in a corrupted state to “keep the game running.”
- **Prefer loud over quiet:** avoid empty `catch`, broad `try/catch` that hides failures, and “best effort” branches that mask missing data or wiring. If something is optional by design, say so in code or a single clear log at the boundary—not scattered silent fallbacks.
- **Invariants:** document and enforce non-negotiable assumptions (e.g. “party list non-null after `EnsureInitialized`”). When an invariant breaks, **stop or assert** rather than patching downstream with defensive coding.
- **Guards vs band-aids:** real robustness (bounds, cycle detection for real edge cases) is fine; **defensive clutter** that only hides bugs is not—see [`scavenger-debugging-workflow`](../scavenger-debugging-workflow/SKILL.md) for root-cause discipline.

## Unity / C# pitfalls

*Reserved.*

## Related

- Repo layout and naming: [`scavenger-conventions`](../scavenger-conventions/SKILL.md)
- Architecture map: [`scavenger-architecture`](../scavenger-architecture/SKILL.md)
- Debugging and root-cause fixes: [`scavenger-debugging-workflow`](../scavenger-debugging-workflow/SKILL.md)
- Post-change verification: [`scavenger-post-change-verification`](../scavenger-post-change-verification/SKILL.md)
