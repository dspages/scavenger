using UnityEngine;
using UnityEngine.UI;

public class PopupTextController : MonoBehaviour
{
    private static PopupText popupTextPrefab;
    private static string canvasName = "Canvas";
    private static GameObject canvas;
    private static PopupTextController runner;

    public static void Initialize()
    {
        canvas = GameObject.Find(canvasName) ?? GameObject.Find("UICanvas");
        if (canvas == null) return;
        var canvasPrefabs = canvas.GetComponent<CanvasPrefabs>();
        if (canvasPrefabs != null)
            popupTextPrefab = canvasPrefabs.popupTextPrefab;
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

    public static void CreatePopupText(string text, Transform targetTransform)
    {
        EnsureInitialized();
        if (canvas == null) return;

        Vector2 screenPosition = Camera.main != null
            ? (Vector2)Camera.main.WorldToScreenPoint(targetTransform.position)
            : new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (popupTextPrefab != null)
        {
            var instance = Instantiate(popupTextPrefab);
            instance.transform.SetParent(canvas.transform, false);
            instance.transform.position = screenPosition;
            instance.SetText(text);
        }
        else
        {
            CreateFallbackPopup(text, screenPosition);
        }
    }

    private static void CreateFallbackPopup(string text, Vector2 screenPosition)
    {
        var go = new GameObject("PopupText_Fallback");
        go.transform.SetParent(canvas.transform, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(150, 40);
        rect.position = screenPosition;

        var textComp = go.AddComponent<Text>();
        textComp.text = text;
        textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComp.fontSize = 24;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.color = Color.white;

        Object.Destroy(go, 1.5f);
    }

    public static void CreatePopupTextAfterDelay(string text, Transform transform, float delay)
    {
        EnsureInitialized();
        runner.StartCoroutine(runner.RunAfterDelay(text, transform, delay));
    }

    private System.Collections.IEnumerator RunAfterDelay(string text, Transform transform, float delay)
    {
        yield return new WaitForSeconds(delay);
        CreatePopupText(text, transform);
    }
}