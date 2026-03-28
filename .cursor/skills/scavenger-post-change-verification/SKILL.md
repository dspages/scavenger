---
name: scavenger-post-change-verification
description: >-
  Defines Scavenger's post-change verification: Unity Console check after C# edits,
  choosing EditMode vs PlayMode tests and correct test folders, running or extending
  tests that cover the changed area, and UnityMCP prerequisites and cleanup before and
  after running tests. Use after nontrivial code changes, when validating fixes or
  checking for regressions, when the user asks to verify changes or run tests safely,
  or after C# edits that need Unity Console review.
---

# Scavenger — post-change verification

Canonical detail lives in **`AGENTS.md`** at the repository root (Unity compiler, test workflow, UnityMCP, cleanup). This skill is the executable checklist.

## When to apply

- Nontrivial logic, UI, or runtime behavior changes.
- User asks for regression checks, verification, or safe test runs.
- Before treating work as done or ready for review.

## Step 1 — Unity Console (after C# changes)

- Use UnityMCP **`read_console`** and/or **`validate_script`** on touched files.
- Address **errors and warnings** tied to the edits by fixing root causes (API usage, invariants, dead code, types)—not by broad `#pragma warning disable` or project-wide suppression.
- Narrow suppressions only when unavoidable (third-party/generated, documented platform limits), smallest scope, with a short comment.

## Step 2 — Tests: type, location, coverage

- Prefer **EditMode** for pure logic and non-scene behavior; **PlayMode** when runtime, scene objects, or frame behavior must be exercised.
- Do not rely on whichever scene is open in the Editor for PlayMode tests—load explicitly in setup.
- **Locations:** EditMode under `Assets/Tests/Editor/`; PlayMode under `Assets/Tests/PlayMode/`; optional committed scene fixtures under `Assets/Tests/Scenes/`.
- For PlayMode, load the required scene in test setup (`EditorSceneManager.LoadSceneInPlayMode` / `LoadSceneAsyncInPlayMode` with full asset path). Production scenes like `Assets/Scenes/MainMenu.unity` or `Assets/Scenes/CombatScene.unity` are OK for integration tests when appropriate.
- Do **not** create temporary `.unity` assets under `Assets/` solely to run a test.
- Prefer **running or adding tests** that cover the changed code paths (regression focus).
- For non-trivial new suites, prefer separate test assemblies with `.asmdef`; PlayMode assemblies must target platforms beyond Editor only.

Full PlayMode loading example: see **`AGENTS.md`** (“Controlling The PlayMode Test Scene”).

## Step 3 — UnityMCP before `run_tests`

- Call **`manage_editor(stop)`** so the editor is not already in Play Mode.
- Poll **`mcpforunity://editor/state`** and start tests only when all are true:
  - `play_mode.is_playing == false`
  - `play_mode.is_changing == false`
  - `tests.is_running == false`
  - `advice.ready_for_tools == true`
- After compilation, domain reload, or a PlayMode transition, allow a short settle period or poll again before **`run_tests`**.

## Step 4 — Cleanup after UnityMCP PlayMode runs

- Confirm the editor has exited Play Mode and is idle.
- Reload the usual working scene (**`Assets/Scenes/MainMenu.unity`**) before ending the task unless the user asked for a different scene left open.
- Treat **`Assets/InitTestScene*.unity`** as tooling artifacts; do not commit temp scenes from tooling. Remove unexpected temp scenes after confirming they are not intentional assets.
