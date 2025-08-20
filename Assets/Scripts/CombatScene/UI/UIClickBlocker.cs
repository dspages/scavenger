using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Simple utility for UI click blocking. Elements with "ui-blocker" class block world interactions.
/// Usage: 
/// - Call IsPointerOverBlockingUI() before processing world clicks
/// - Call MakeElementBlockClicks(element) on panels that should block
/// </summary>
public static class UIClickBlocker
{
    public static bool IsPointerOverBlockingUI()
    {
        var docs = Object.FindObjectsOfType<UIDocument>();
        if (docs == null || docs.Length == 0) return false;

        Vector2 mousePos = Input.mousePosition;
        for (int i = 0; i < docs.Length; i++)
        {
            var root = docs[i].rootVisualElement;
            if (root == null || root.panel == null) continue;
            
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, mousePos);
            panelPos.y = Screen.height - panelPos.y;  // Flip Y for UI Toolkit
            
            var picked = root.panel.Pick(panelPos);
            if (picked != null && picked.ClassListContains("ui-blocker"))
            {
                return true;
            }
        }
        return false;
    }

    public static void MakeElementBlockClicks(VisualElement element)
    {
        if (element == null) return;
        element.AddToClassList("ui-blocker");
        element.pickingMode = PickingMode.Position;
    }

    /// <summary>
    /// Makes an element and ALL its children block clicks recursively.
    /// Use this for panels where you want everything inside to block world interactions.
    /// </summary>
    public static void MakeElementAndChildrenBlockClicks(VisualElement element)
    {
        if (element == null) return;
        
        // Make this element block clicks
        MakeElementBlockClicks(element);
        
        // Recursively apply to all children
        for (int i = 0; i < element.childCount; i++)
        {
            MakeElementAndChildrenBlockClicks(element[i]);
        }
    }
}
