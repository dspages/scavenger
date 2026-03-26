using UnityEngine;
using UnityEngine.UI;

public class PopupTextController : MonoBehaviour
{
    private static PopupText popupTextPrefab;
    private static string canvasName = "Canvas";
    private static GameObject canvas;
    private static PopupTextController runner;
    private const float AboveTargetOffsetYPixels = 40f;
    private const float RightOffsetXPixels = AboveTargetOffsetYPixels * 1.5f;

    public static void Initialize()
    {
        // Search all loaded scenes for a CanvasPrefabs that has the popup prefab assigned (e.g. Main Menu has it, Combat might not)
        var all = Object.FindObjectsByType<CanvasPrefabs>(
            findObjectsInactive: FindObjectsInactive.Include,
            sortMode: FindObjectsSortMode.None);
        foreach (var cp in all)
        {
            if (cp.popupTextPrefab != null)
            {
                popupTextPrefab = cp.popupTextPrefab;
                canvas = cp.gameObject;
                return;
            }
        }
        // Fallback: use first Canvas/UICanvas and its prefab (may be null)
        canvas = GameObject.Find(canvasName) ?? GameObject.Find("UICanvas");
        if (canvas != null)
        {
            var canvasPrefabs = canvas.GetComponent<CanvasPrefabs>();
            if (canvasPrefabs != null)
                popupTextPrefab = canvasPrefabs.popupTextPrefab;
        }
    }

    private static void EnsureInitialized()
    {
        if (canvas == null || popupTextPrefab == null)
        {
            Initialize();
        }
        if (runner == null)
        {
            var go = GameObject.Find("PopupTextControllerRunner");
            if (go == null)
            {
                go = new GameObject("PopupTextControllerRunner");
                runner = go.AddComponent<PopupTextController>();
                DontDestroyOnLoad(go);
            }
            else
            {
                runner = go.GetComponent<PopupTextController>();
                if (runner == null)
                {
                    runner = go.AddComponent<PopupTextController>();
                }
            }
        }
    }

    /// <summary>Primary API: show popup with optional color. Fails clearly if prefab or canvas is missing (no silent fallback).</summary>
    public static void CreatePopupText(string text, Transform targetTransform, Color? color = null)
    {
        EnsureInitialized();
        if (canvas == null)
        {
            Debug.LogError("PopupTextController: No canvas found (looked for 'Canvas' or 'UICanvas'). Add a Canvas to the scene and assign it.");
            return;
        }
        if (popupTextPrefab == null)
        {
            Debug.LogError("PopupTextController: PopupText prefab is not assigned. Add CanvasPrefabs to your UICanvas and assign the PopupText prefab.");
            return;
        }

        var canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            Debug.LogError("PopupTextController: Canvas is missing RectTransform.");
            return;
        }

        Vector2 screenPosition;
        if (Camera.main != null)
        {
            var screen3 = Camera.main.WorldToScreenPoint(targetTransform.position);
            if (screen3.z < 0f)
                return; // Behind the camera; no sensible screen position.
            screenPosition = new Vector2(screen3.x, screen3.y);
        }
        else
        {
            screenPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
        }

        var instance = Instantiate(popupTextPrefab);
        instance.transform.SetParent(canvas.transform, false);
        // Revert to transform.position assignment (known-good visibility).
        // We still apply a screen-space Y offset so the popup appears above the target.
        screenPosition.x += RightOffsetXPixels;
        screenPosition.y += AboveTargetOffsetYPixels;
        instance.transform.position = screenPosition;

        instance.SetText(text);
        if (color.HasValue)
            instance.SetColor(color.Value);
    }

    /// <summary>Show damage popup with damage-type color. Use for hit feedback.</summary>
    public static void CreateDamagePopup(int damage, bool critical, EquippableHandheld.DamageType damageType, Transform targetTransform)
    {
        string text = critical ? $"CRIT {damage}" : damage.ToString();
        CreatePopupText(text, targetTransform, DamageTypeColors.Get(damageType));
    }

    /// <summary>Show MISS popup with neutral color.</summary>
    public static void CreateMissPopup(Transform targetTransform)
    {
        CreatePopupText("MISS", targetTransform, DamageTypeColors.MissColor);
    }

    public static void CreatePopupTextAfterDelay(string text, Transform transform, float delay, Color? color = null)
    {
        EnsureInitialized();
        if (runner != null)
            runner.StartCoroutine(runner.RunAfterDelay(text, transform, delay, color));
    }

    private System.Collections.IEnumerator RunAfterDelay(string text, Transform transform, float delay, Color? color = null)
    {
        yield return new WaitForSeconds(delay);
        CreatePopupText(text, transform, color);
    }
}