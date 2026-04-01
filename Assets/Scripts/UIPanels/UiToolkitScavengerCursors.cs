using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Runtime UITK cursors using <see cref="Resources"/> textures under <c>ScavengerCursors/</c> (CPU-readable, Cursor type).
/// USS <c>cursor: url(...)</c> is unreliable for OS cursors in player builds; use this for hover affordances.
/// </summary>
/// <remarks>
/// UITK does not inherit <see cref="IStyle.cursor"/> from parent to child: the element under the pointer
/// wins. For a uniform cursor over a tile, set <see cref="VisualElement.pickingMode"/> to <see cref="PickingMode.Ignore"/>
/// on decorative children (icons, labels) so the parent receives the hover, or set the same cursor on each child.
/// </remarks>
public static class UiToolkitScavengerCursors
{
    static Texture2D _handPoint;
    static Texture2D _gauntletOpen;

    static readonly Vector2 HandHotspot = new Vector2(4f, 4f);
    static readonly Vector2 GauntletHotspot = new Vector2(4f, 4f);

    /// <summary>Hand / “point” cursor: buttons, tabs, mission cards (click-to-select), tooltips on text.</summary>
    public static void RegisterHandPointerHover(VisualElement element)
    {
        if (element == null) return;
        element.RegisterCallback<PointerEnterEvent>(_ => ApplyHandCursor(element));
        element.RegisterCallback<PointerLeaveEvent>(_ => ClearElementCursor(element));
    }

    /// <summary>Alias for <see cref="RegisterHandPointerHover"/> — use for click-only affordances.</summary>
    public static void RegisterClickPointerHover(VisualElement element) => RegisterHandPointerHover(element);

    /// <summary>Open gauntlet: floating panel drag handles, or alias for drag/drop surfaces.</summary>
    public static void RegisterGauntletPointerHover(VisualElement element)
    {
        if (element == null) return;
        element.RegisterCallback<PointerEnterEvent>(_ => ApplyGauntletCursor(element));
        element.RegisterCallback<PointerLeaveEvent>(_ => ClearElementCursor(element));
    }

    /// <summary>Gauntlet (grab) cursor for drag sources, drop targets, and inventory tiles. Falls back to hand if gauntlet texture missing.</summary>
    public static void RegisterDragAffordancePointerHover(VisualElement element) => RegisterGauntletPointerHover(element);

    /// <summary>
    /// Backpack / equipment tiles: gauntlet only when the slot contains an item (draggable); hand when empty.
    /// Predicate is evaluated on each pointer enter so it stays correct after inventory changes.
    /// </summary>
    public static void RegisterDraggableItemSlotPointerHover(VisualElement element, System.Func<bool> hasDraggableItem)
    {
        if (element == null || hasDraggableItem == null) return;
        element.RegisterCallback<PointerEnterEvent>(_ =>
        {
            if (hasDraggableItem())
                ApplyGauntletCursor(element);
            else
                ApplyHandCursor(element);
        });
        element.RegisterCallback<PointerLeaveEvent>(_ => ClearElementCursor(element));
    }

    static void ApplyHandCursor(VisualElement element)
    {
        var tex = GetHandPointTexture();
        if (tex == null) return;
        element.style.cursor = new StyleCursor(new UnityEngine.UIElements.Cursor
        {
            texture = tex,
            hotspot = HandHotspot,
        });
    }

    static void ApplyGauntletCursor(VisualElement element)
    {
        Texture2D gauntletTex = GetGauntletOpenTexture();
        Texture2D tex = gauntletTex != null ? gauntletTex : GetHandPointTexture();
        if (tex == null) return;
        element.style.cursor = new StyleCursor(new UnityEngine.UIElements.Cursor
        {
            texture = tex,
            hotspot = gauntletTex != null ? GauntletHotspot : HandHotspot,
        });
    }

    static void ClearElementCursor(VisualElement element)
    {
        element.style.cursor = new StyleCursor(StyleKeyword.Null);
    }

    static Texture2D GetHandPointTexture()
    {
        if (_handPoint != null) return _handPoint;
        _handPoint = LoadTexture("ScavengerCursors/hand_point");
        return _handPoint;
    }

    static Texture2D GetGauntletOpenTexture()
    {
        if (_gauntletOpen != null) return _gauntletOpen;
        _gauntletOpen = LoadTexture("ScavengerCursors/gauntlet_open");
        return _gauntletOpen;
    }

    static Texture2D LoadTexture(string resourcePath)
    {
        Texture2D tex = Resources.Load<Texture2D>(resourcePath);
        if (tex != null) return tex;
        Sprite sp = Resources.Load<Sprite>(resourcePath);
        return sp != null ? sp.texture : null;
    }
}
