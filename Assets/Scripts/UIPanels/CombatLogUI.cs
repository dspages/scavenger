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

    private bool isDragging;
    private Vector2 dragOffset;
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
        SetupDragHandle(root);
        SetupMinimizeToggle();

        CombatLog.OnEntryAdded += OnLogEntry;

        foreach (var entry in CombatLog.Entries)
            AppendLine(entry);
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

    private void SetupDragHandle(VisualElement root)
    {
        var handle = root.Q<VisualElement>("CombatLogDragHandle");
        if (handle == null || panel == null) return;

        handle.RegisterCallback<PointerDownEvent>(evt =>
        {
            isDragging = true;
            dragOffset = evt.localPosition;
            handle.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        });

        handle.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (!isDragging) return;

            var parentRect = panel.parent.contentRect;
            var panelRect = panel.layout;

            float newLeft = panel.resolvedStyle.left + evt.localPosition.x - dragOffset.x;
            float newTop = panel.resolvedStyle.top + evt.localPosition.y - dragOffset.y;

            newLeft = Mathf.Clamp(newLeft, 0, parentRect.width - panelRect.width);
            newTop = Mathf.Clamp(newTop, 0, parentRect.height - panelRect.height);

            panel.style.left = newLeft;
            panel.style.top = newTop;
            panel.style.bottom = StyleKeyword.Auto;
            panel.style.right = StyleKeyword.Auto;

            evt.StopPropagation();
        });

        handle.RegisterCallback<PointerUpEvent>(evt =>
        {
            isDragging = false;
            handle.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        });
    }

    private void OnLogEntry(string message)
    {
        AppendLine(message);
    }

    private void AppendLine(string text)
    {
        if (logLabel == null) return;

        if (!string.IsNullOrEmpty(logLabel.text))
            logLabel.text += "\n";
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
