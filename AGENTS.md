# Agent Instructions

## Unity compiler errors and warnings

- After changing C# (or when the user reports build issues), **check the Unity Console** (via UnityMCP `read_console` and/or `validate_script` on touched files) for **errors and warnings** tied to your edits.
- **Prefer fixing** the underlying issue (correct API usage, null checks that reflect real invariants, remove dead code, adjust types) over **suppressing** diagnostics (`#pragma warning disable`, blanket warning ignores, hiding messages, or turning rules off project-wide).
- Suppressions are only appropriate when unavoidable (third-party/generated code, documented platform limitation) and should be **narrow** (single line or smallest scope) with a **short comment** explaining why.

## Intended Codepath Over Fallbacks

- Prefer fixing the intended (happy) path over adding or relying on fallback behavior. Do not bloat the codebase with unnecessary fallback codepaths.
- When a feature depends on assets (e.g. prefabs, scene references), document what the user must add to the scene or project so the intended path works; do not treat "missing prefab" or "missing reference" as a reason to add permanent fallback code that hides the problem.
- If something does not work (e.g. popup not visible), debug until the real cause is found and the intended codepath runs correctly—fix references, add missing prefabs to the scene, or fix the prefab/scene setup—rather than papering over it with alternate code paths.

## Root Cause Over Symptom

- Do not wallpaper over problems with band-aids or tech debt. It is unacceptable to "fix" bugs by adding caps, step limits, or defensive guards without diagnosing and fixing the underlying cause.
- When fixing a bug, diagnose why the incorrect behavior occurs (e.g. infinite loop, wrong data, wrong control flow), then fix that root cause. Even if that is more difficult than only addressing the symptom, the proper fix is required.
- Safety guards (e.g. cycle detection, bounds checks) are acceptable when they make the system robust against real edge cases, but they must not substitute for fixing the actual bug (e.g. ensuring pathfinding cannot produce cycles, not only breaking when one is detected).

## Unity Test Workflow

- Prefer `EditMode` tests for pure logic and non-scene behavior.
- Prefer `PlayMode` tests only when Unity runtime behavior, scene objects, or frame-based behavior must be exercised.
- Do not rely on whichever scene happens to be open in the Editor when starting a PlayMode test.

## Test Locations

- EditMode tests belong under `Assets/Tests/Editor/`.
- PlayMode tests belong under `Assets/Tests/PlayMode/`.
- If a PlayMode test needs a scene fixture, prefer a committed test scene under `Assets/Tests/Scenes/`.

## Controlling The PlayMode Test Scene

- Standard Unity Test Framework practice is to load the required scene explicitly from the test setup.
- For PlayMode tests running in the Editor, prefer `EditorSceneManager.LoadSceneInPlayMode()` or `LoadSceneAsyncInPlayMode()` with a full asset path.
- Use `[UnitySetUp]` for per-test scene loading or `[UnityOneTimeSetUp]` for one scene load shared by a whole fixture.
- If testing a real game scene, it is acceptable to load a committed production scene such as `Assets/Scenes/MainMenu.unity` or `Assets/Scenes/CombatScene.unity` explicitly from setup.
- Do not create temporary `.unity` scene assets under `Assets/` just to run a test.

Example pattern:

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

## Isolated Vs Real-Scene Tests

- For isolated PlayMode tests, use a dedicated committed scene fixture in `Assets/Tests/Scenes/`.
- For integration tests, load the real committed scene that the game actually uses.
- If a test only needs temporary runtime objects, prefer creating them in memory at runtime rather than saving a new scene asset.

## UnityMCP Test Execution

- Before running PlayMode tests through UnityMCP, call `manage_editor(stop)` to ensure the editor is not already in Play Mode.
- Poll `mcpforunity://editor/state` and only start PlayMode tests when all of the following are true:
- `play_mode.is_playing == false`
- `play_mode.is_changing == false`
- `tests.is_running == false`
- `advice.ready_for_tools == true`
- If Unity has just completed compilation, domain reload, or a PlayMode transition, allow a short settle period or poll state once more before calling `run_tests`.

## Cleanup After UnityMCP PlayMode Runs

- After PlayMode tests complete, confirm the editor has exited Play Mode and returned to an idle state.
- Reload the expected working scene, usually `Assets/Scenes/MainMenu.unity`, before ending the task unless the user asked to leave a different scene open.
- If any unexpected temp scene assets such as `Assets/InitTestScene*.unity` appear, treat them as tooling artifacts rather than source files.
- Do not commit temp scenes created by tooling.
- If a temp scene was created unexpectedly, remove it after confirming it is not an intentional project asset.

## Assembly Guidance

- Prefer separate test assemblies with `.asmdef` files for EditMode and PlayMode tests when adding more than a trivial smoke test.
- A PlayMode test assembly must target platforms beyond `Editor`; an Editor-only assembly is an EditMode assembly.

## UI Toolkit floating and docked panels

- **Floating panels** (combat log, popped inventory, future loot/corpse transfer): use a top **chrome** row—title, **drag handle** (name `PanelDragHandle` when adding new UXML, or follow existing `CombatLogDragHandle`), optional minimize/close. Implement dragging with [`PanelDragController`](Assets/Scripts/UIPanels/PanelDragController.cs) (`Attach()` once after resolving panel + handle).
- **Docked panels** (e.g. Home Base preparation columns): reuse the same **visual** chrome where helpful—USS classes `game-floating-panel__header` (compact drag strip) and `game-floating-panel__title-row` (taller title + actions) live in [`CombatPanel.uss`](Assets/UI/UXML/CombatPanel.uss). Dragging is optional for docked layouts.
- **Two-sided transfer** (stash, chests, corpses): mirror [`HomeBaseLoadoutPresenter`](Assets/Scripts/UIPanels/HomeBaseLoadoutPresenter.cs) + [`DragDropController`](Assets/Scripts/UIPanels/Inventory/DragDropController.cs); see [`InventoryTransferUiPattern`](Assets/Scripts/UIPanels/InventoryTransferUiPattern.cs) summary.
