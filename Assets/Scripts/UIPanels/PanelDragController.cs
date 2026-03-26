using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Drags a floating <see cref="VisualElement"/> panel within its parent's bounds by pointer capture on a handle.
/// </summary>
/// <remarks>
/// <para><b>Project convention</b> (see AGENTS.md): floating panels use a top header strip with optional title,
/// drag region, minimize, and close. Name drag roots <c>PanelDragHandle</c> when practical; combat log uses
/// <c>CombatLogDragHandle</c> for history. Docked panels (e.g. Home Base prep columns) may use the same
/// visual header classes without calling <see cref="Attach"/>.</para>
/// </remarks>
public sealed class PanelDragController
{
    private readonly VisualElement panel;
    private readonly VisualElement handle;
    private bool isDragging;
    private Vector2 dragOffset;

    public PanelDragController(VisualElement panel, VisualElement handle)
    {
        this.panel = panel;
        this.handle = handle;
    }

    /// <summary>Registers pointer callbacks on <see cref="handle"/> (call once).</summary>
    public void Attach()
    {
        if (handle == null || panel == null) return;

        handle.RegisterCallback<PointerDownEvent>(OnPointerDown);
        handle.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        handle.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        isDragging = true;
        dragOffset = evt.localPosition;
        handle.CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
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
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        isDragging = false;
        handle.ReleasePointer(evt.pointerId);
        evt.StopPropagation();
    }
}
