using UnityEngine;
using UnityEngine.UIElements;

public class CombatLogUI : MonoBehaviour
{
    private const float ExpandedWidth = 300f;
    private const float ExpandedHeight = 140f;
    private const float MinimizedWidth = 36f;
    private const float MinimizedHeight = 28f;

    private Label logLabel;
    private ScrollView scrollView;
    private VisualElement panel;
    private VisualElement scrollContainer;
    private VisualElement dragHandleLabel;
    private Button minimizeButton;

    private bool isMinimized;

    public void Initialize(VisualElement root)
    {
        panel = root.Q<VisualElement>("CombatLogPanel");
        scrollView = root.Q<ScrollView>("CombatLogScroll");
        logLabel = root.Q<Label>("CombatLogText");
        minimizeButton = root.Q<Button>("CombatLogMinimizeButton");
        dragHandleLabel = root.Q<VisualElement>("CombatLogTitle");

        if (logLabel != null)
            logLabel.text = "";

        // Ensure scroll view and its vertical scroller block world clicks (scroll bar was transparent)
        if (scrollView != null)
        {
            UIClickBlocker.MakeElementAndChildrenBlockClicks(scrollView);
            if (scrollView.verticalScroller != null)
                UIClickBlocker.MakeElementAndChildrenBlockClicks(scrollView.verticalScroller);
        }

        scrollContainer = scrollView; // the part we hide when minimized
        var dragHandle = root.Q<VisualElement>("CombatLogDragHandle");
        if (dragHandle != null && panel != null)
            new PanelDragController(panel, dragHandle).Attach();
        SetupMinimizeToggle();

        CombatLog.OnEntryAdded += OnLogEntry;

        for (int i = 0; i < CombatLog.Entries.Count; i++)
            AppendLine(CombatLog.Entries[i], CombatLog.GetEntryDamageType(i));
    }

    private void SetupMinimizeToggle()
    {
        if (minimizeButton == null || panel == null || scrollContainer == null) return;

        isMinimized = false;
        minimizeButton.clicked += () =>
        {
            isMinimized = !isMinimized;
            if (isMinimized)
            {
                panel.style.width = MinimizedWidth;
                panel.style.height = MinimizedHeight;
                scrollContainer.style.display = DisplayStyle.None;
                if (dragHandleLabel != null) dragHandleLabel.style.display = DisplayStyle.None;
                minimizeButton.text = "\u25B2"; // ▲ expand
                minimizeButton.tooltip = "Expand combat log";
            }
            else
            {
                panel.style.width = ExpandedWidth;
                panel.style.height = ExpandedHeight;
                scrollContainer.style.display = DisplayStyle.Flex;
                if (dragHandleLabel != null) dragHandleLabel.style.display = DisplayStyle.Flex;
                minimizeButton.text = "\u25BC"; // ▼ minimize
                minimizeButton.tooltip = "Minimize combat log";
            }
        };
    }

    private void OnDestroy()
    {
        CombatLog.OnEntryAdded -= OnLogEntry;
    }

    private void OnLogEntry(string message, EquippableHandheld.DamageType? damageType)
    {
        AppendLine(message, damageType);
    }

    private void AppendLine(string text, EquippableHandheld.DamageType? damageType = null)
    {
        if (logLabel == null) return;

        logLabel.enableRichText = true;
        if (!string.IsNullOrEmpty(logLabel.text))
            logLabel.text += "\n";
        if (damageType.HasValue)
            logLabel.text += $"<color=#{DamageTypeColors.GetHex(damageType.Value)}>{text}</color>";
        else
            logLabel.text += text;

        if (scrollView != null)
        {
            scrollView.schedule.Execute(() =>
            {
                scrollView.scrollOffset = new Vector2(0, scrollView.contentContainer.layout.height);
            });
        }
    }
}
