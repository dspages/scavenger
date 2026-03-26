using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Opens game scenes in the Editor. Unity's Hierarchy only lists scenes that are currently loaded;
/// use <b>Scavenger/Scenes/Load Main Menu + Home Base + Combat (multi-scene)</b> to see all three at once.
/// </summary>
public static class ScavengerSceneMenu
{
    const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    const string HomeBasePath = "Assets/Scenes/HomeBase.unity";
    const string CombatPath = "Assets/Scenes/CombatScene.unity";

    [MenuItem("Scavenger/Scenes/Open Main Menu", false, 1)]
    public static void OpenMainMenu() => OpenSingle(MainMenuPath);

    [MenuItem("Scavenger/Scenes/Open Home Base", false, 2)]
    public static void OpenHomeBase() => OpenSingle(HomeBasePath);

    [MenuItem("Scavenger/Scenes/Open Combat", false, 3)]
    public static void OpenCombat() => OpenSingle(CombatPath);

    [MenuItem("Scavenger/Scenes/Load Main Menu + Home Base + Combat (multi-scene)", false, 11)]
    public static void OpenAllAdditive()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        const string msg =
            "This loads all three game scenes additively so they all appear in the Hierarchy.\n\n" +
            "You will have multiple Main Cameras and EventSystems until you disable or remove extras in the non-active scenes. " +
            "Do not use Play Mode with every duplicate left enabled.\n\n" +
            "Continue?";

        if (!EditorUtility.DisplayDialog("Load multi-scene layout", msg, "Load", "Cancel"))
            return;

        EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
        EditorSceneManager.OpenScene(HomeBasePath, OpenSceneMode.Additive);
        EditorSceneManager.OpenScene(CombatPath, OpenSceneMode.Additive);
    }

    static void OpenSingle(string path)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }
}
