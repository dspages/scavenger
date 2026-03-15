using UnityEngine;
using System.IO;

public static class SaveManager
{
    private const string SaveFileName = "savegame.json";

    public static string SaveFilePath =>
        Path.Combine(Application.persistentDataPath, SaveFileName);

    public static bool SaveExists() => File.Exists(SaveFilePath);

    public static bool Save()
    {
        try
        {
            var data = GameSaveData.FromCurrentState();
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SaveFilePath, json);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Save failed: {e.Message}");
            return false;
        }
    }

    public static bool Load()
    {
        if (!SaveExists()) return false;
        try
        {
            string json = File.ReadAllText(SaveFilePath);
            var data = JsonUtility.FromJson<GameSaveData>(json);
            data.RestoreState();
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Load failed: {e.Message}");
            return false;
        }
    }

    public static void DeleteSave()
    {
        if (SaveExists()) File.Delete(SaveFilePath);
    }
}
