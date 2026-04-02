---
name: scavenger-post-change-verification
description: >-
  After-change checklist: self-review/DRY, Unity Console, choose & run tests, UnityMCP
  run_tests readiness and cleanup. Use after nontrivial changes or when asked to verify.
---

# Scavenger — post-change verification

Canonical verification checklist after changes. For “what tests/docs should I add?”, see **`scavenger-change-companion`**.

## When to apply

- Nontrivial logic, UI, or runtime behavior changes.
- User asks for regression checks, verification, or safe test runs.
- Before treating work as done or ready for review.

## Step 1 — Self-review and DRY (your own change)

- **Re-read the diff** (or changed files): names, control flow, obvious redundancies, and whether the change matches project patterns in nearby code.
- **Search for existing helpers** before leaving new logic in place: look for the same operation already implemented (utilities, extension methods, existing services, parallel types). Prefer **reusing or extending** shared code over **copy-paste** variants.
- If you inlined something that duplicates behavior elsewhere, **refactor to a single implementation** (or a thin wrapper) when the cost is reasonable—do not ship near-duplicates “to save time” when consolidation is straightforward.
- **Remove** dead code, stray debug logging, and commented-out blocks introduced during iteration unless they are intentionally kept with context.

## Step 2 — Unity Console (after C# changes)

- If **Unity MCP is down or tools fail**, **tell the user loudly** (do not act like verification passed). They need to start/reconnect the Unity MCP server in Cursor.
- Use UnityMCP **`read_console`** and/or **`validate_script`** on touched files.
- Address **errors and warnings** tied to the edits by fixing root causes (API usage, invariants, dead code, types)—not by broad `#pragma warning disable` or project-wide suppression.
- Narrow suppressions only when unavoidable (third-party/generated, documented platform limits), smallest scope, with a short comment.

## Step 3 — Tests: type, location, coverage

- Prefer **EditMode** for pure logic and non-scene behavior; **PlayMode** when runtime, scene objects, or frame behavior must be exercised.
- Do not rely on whichever scene is open in the Editor for PlayMode tests—load explicitly in setup.
- **Locations:** EditMode under `Assets/Tests/Editor/`; PlayMode under `Assets/Tests/PlayMode/`; optional committed scene fixtures under `Assets/Tests/Scenes/`.
- For PlayMode, load the required scene in test setup (`EditorSceneManager.LoadSceneInPlayMode` / `LoadSceneAsyncInPlayMode` with a full asset path). Use `[UnitySetUp]` for per-test loading or `[UnityOneTimeSetUp]` when one scene load is shared by a whole fixture. Production scenes such as `Assets/Scenes/MainMenu.unity` or `Assets/Scenes/CombatScene.unity` are OK for integration tests when appropriate.
- Do **not** create temporary `.unity` assets under `Assets/` solely to run a test.
- **Isolated vs integration:** for isolated PlayMode tests, use a dedicated committed scene in `Assets/Tests/Scenes/`. For integration tests, load the real committed scene the game uses. If a test only needs temporary runtime objects, create them in memory rather than saving a new scene asset.
- Prefer **running or adding tests** that cover the changed code paths (regression focus).
- Prefer separate test assemblies with `.asmdef` for EditMode and PlayMode when adding more than a trivial smoke test. A PlayMode test assembly must target platforms beyond `Editor`; an Editor-only assembly is an EditMode assembly.

Example — explicit PlayMode scene load:

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class MainMenuIntegrationTests
{
    [UnitySetUp]
    public IEnumerator LoadScene()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
            "Assets/Scenes/MainMenu.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
    }

    [UnityTest]
    public IEnumerator MainMenuScene_Loads()
    {
        Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
        yield return null;
    }
}
```

## Step 4 — UnityMCP before `run_tests`

- Call **`manage_editor(stop)`** so the editor is not already in Play Mode.
- Poll **`mcpforunity://editor/state`** and start tests only when all are true:
  - `play_mode.is_playing == false`
  - `play_mode.is_changing == false`
  - `tests.is_running == false`
  - `advice.ready_for_tools == true`
- After compilation, domain reload, or a PlayMode transition, allow a short settle period or poll again before **`run_tests`**.

## Step 5 — Cleanup after UnityMCP PlayMode runs

- Confirm the editor has exited Play Mode and is idle.
- Reload the usual working scene (**`Assets/Scenes/MainMenu.unity`**) before ending the task unless the user asked for a different scene left open.
- Treat **`Assets/InitTestScene*.unity`** as tooling artifacts; do not commit temp scenes from tooling. Remove unexpected temp scenes after confirming they are not intentional assets.
