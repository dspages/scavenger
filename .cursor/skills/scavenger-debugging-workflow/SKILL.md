---
name: scavenger-debugging-workflow
description: >-
  Bug-fix discipline: keep the intended path working, find root cause, avoid permanent
  fallbacks/defensive clutter. Use when debugging or fixing incorrect behavior.
---

# Scavenger — debugging workflow

## When to apply

- Something behaves incorrectly, is invisible, or fails at runtime or in the Editor.
- The user asks for a proper fix, root cause, or to avoid hacks.
- Before adding caps, step limits, or broad try/catch or null-guards “just in case.”

## Intended codepath over fallbacks

- Prefer fixing the **intended (happy) path** over adding or relying on fallback behavior. Do not bloat the codebase with unnecessary fallback codepaths.
- When a feature depends on **assets** (prefabs, scene references, UI documents), **document** what must exist in the scene or project so the intended path works. Do not treat “missing prefab” or “missing reference” as a reason to add **permanent** fallback code that hides the problem.
- If something does not work (e.g. a popup not visible), **debug until the real cause** is found and the intended codepath runs: fix references, wire missing prefabs/scenes, or correct prefab/scene setup—rather than papering over it with alternate code paths.

## Root cause over symptom

- Do not wallpaper over problems with **band-aids** or tech debt. It is unacceptable to “fix” bugs by adding caps, step limits, or defensive guards **without** diagnosing and fixing the underlying cause.
- When fixing a bug, **diagnose why** the incorrect behavior occurs (e.g. infinite loop, wrong data, wrong control flow), then fix **that** root cause. Even when that is harder than patching the symptom, the proper fix is required.
- **Only as an absolute last resort**—when you cannot get enough signal from the codebase, Editor, or tests alone—you may **involve the user** in root-cause analysis: add **temporary** diagnostic logging, ask them to **reproduce** the bug once, then inspect results via UnityMCP **`read_console`** (or ask them to paste the matching Unity Console lines). Strip or narrow logs afterward so they do not become permanent noise.
- **Safety guards** (e.g. cycle detection, bounds checks) are acceptable when they make the system robust against **real** edge cases, but they must **not** substitute for fixing the actual bug (e.g. ensuring pathfinding cannot produce cycles, not only breaking when one is detected).

## Suggested sequence

1. **Reproduce** reliably (Editor vs player, which scene, which steps).
2. **Localize** the failure (data, logic, scene/UI wiring, timing).
3. **Fix the cause** on the intended path; restore or add missing references/assets when that is the issue.
4. **Verify** using [`scavenger-post-change-verification`](../scavenger-post-change-verification/SKILL.md) when the fix touches code that should be compiled or tested.
5. **Never abandon work silently**: if you cannot complete a requested task, either (a) complete the parts you can, or (b) enter a clear debugging loop; if you are still blocked on a specific external issue, **escalate to the user** by naming the blocker, what you already tried, and (when possible) proposing a new skill or protocol that would let you handle this autonomously next time.
