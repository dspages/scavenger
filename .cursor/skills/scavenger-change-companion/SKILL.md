---
name: scavenger-change-companion
description: >-
  What to update alongside ordinary code changes: minimal regression tests (EditMode vs
  PlayMode), light touches to skills or the player guide when facts drift, and when to
  read reference.md for large refactors. Use after a feature or fix that is not a full
  subsystem rewrite, or when the user asks to update tests and docs with a change.
---

# Scavenger — change companion (ordinary changes)

For **large or cross-cutting** refactors (architecture, save schema, repo-wide rules, human docs overhaul), read **[`reference.md`](reference.md)** first.

## When to apply

- After implementing a feature or fix that is **not** a whole-subsystem rewrite.
- User asks to “update tests/docs” or finish a PR-sized change with appropriate coverage.
- An existing **skill** has a wrong path or outdated rule after your edit—fix **that** skill file only.

## Regression tests (basics)

- Prefer **EditMode** tests for pure logic under [`Assets/Tests/Editor/`](../../../Assets/Tests/Editor/).
- Use **PlayMode** only when Unity runtime, scene objects, or frame-based behavior must be exercised—under [`Assets/Tests/PlayMode/`](../../../Assets/Tests/PlayMode/), with committed scenes under [`Assets/Tests/Scenes/`](../../../Assets/Tests/Scenes/) when needed.
- Add or extend **minimal** tests that would have failed before the bug fix, or that lock new behavior: typically one happy path plus an obvious edge case when cheap.
- **Running the Unity Console check, choosing test type details, UnityMCP `run_tests` gates, and PlayMode cleanup** live in [`scavenger-post-change-verification`](../scavenger-post-change-verification/SKILL.md)—follow that skill for execution; do not duplicate it here.

## Light documentation

- **Player-facing:** If behavior is user-visible and stable, add a **short** bullet to [`docs/PLAYER_GUIDE.md`](../../../docs/PLAYER_GUIDE.md). Skip essay-length updates for tiny tweaks.
- **Skills:** Correct outdated facts in the **one** affected skill; do not rewrite every skill for a small change.
- **New skills:** When adding a skill, add one row to [`AGENTS.md`](../../../AGENTS.md).

## Related

- Verification checklist: [`scavenger-post-change-verification`](../scavenger-post-change-verification/SKILL.md)
- Large changes: [`reference.md`](reference.md)
