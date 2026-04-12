using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Play Mode smoke tests: major scenes load and retain stable, high-level wiring (UI roots, combat core).
/// Uses <see cref="SceneManager.LoadSceneAsync(string, LoadSceneMode)"/> for runtime-compatible Play Mode loading.
/// </summary>
[TestFixture]
public class SceneSmokeTests
{
    [SetUp]
    public void SetUp()
    {
        PlayerParty.EnsureInitialized();
    }

    private static IEnumerator LoadSceneAndYield(string shortName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(shortName, LoadSceneMode.Single);
        Assert.IsNotNull(op);
        yield return op;
        var loaded = SceneManager.GetSceneByName(shortName);
        Assert.IsTrue(loaded.IsValid() && loaded.isLoaded,
            $"Scene '{shortName}' should be loaded");
        yield return null;
    }

    [UnityTest]
    public IEnumerator MainMenu_Loads_WithMainMenuUIAndInput()
    {
        yield return LoadSceneAndYield(SceneNames.MainMenu);

        var menu = Object.FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Exclude);
        Assert.IsNotNull(menu, "MainMenu scene should contain MainMenuUI");
        Assert.IsNotNull(menu.MainMenuTemplate, "MainMenuUI.MainMenuTemplate should be assigned in the scene");

        Assert.IsNotNull(
            Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Exclude),
            "EventSystem should exist for UI input");
    }

    [UnityTest]
    public IEnumerator HomeBase_Loads_WithHomeBaseUIAndInput()
    {
        yield return LoadSceneAndYield(SceneNames.HomeBase);

        var home = Object.FindFirstObjectByType<HomeBaseUI>(FindObjectsInactive.Exclude);
        Assert.IsNotNull(home, "HomeBase scene should contain HomeBaseUI");
        Assert.IsNotNull(home.HomeBaseTemplate, "HomeBaseUI.HomeBaseTemplate should be assigned in the scene");

        Assert.IsNotNull(
            Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Exclude),
            "EventSystem should exist for UI input");
    }

    [UnityTest]
    public IEnumerator CombatScene_Loads_WithCombatCoreAndPanel()
    {
        yield return LoadSceneAndYield(SceneNames.Combat);

        var turn = Object.FindFirstObjectByType<TurnManager>(FindObjectsInactive.Exclude);
        Assert.IsNotNull(turn, "Combat scene should contain TurnManager");

        var tiles = Object.FindFirstObjectByType<TileManager>(FindObjectsInactive.Exclude);
        Assert.IsNotNull(tiles, "Combat scene should contain TileManager");

        var panel = Object.FindFirstObjectByType<CombatPanelUI>(FindObjectsInactive.Exclude);
        Assert.IsNotNull(panel, "Combat scene should contain CombatPanelUI");
        Assert.IsNotNull(panel.CombatMenuTemplate, "CombatPanelUI.CombatMenuTemplate should be assigned in the scene");

        Assert.IsNotNull(
            Object.FindFirstObjectByType<VisionSystem>(FindObjectsInactive.Exclude),
            "Combat scene should contain VisionSystem");

        Assert.IsNotNull(
            Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Exclude),
            "EventSystem should exist for UI input");
    }
}
