---
name: scavenger-ui-design
description: UITK interaction design: cursors, hover/click/drag affordances, and cursor import pitfalls. Use to keep Main Menu/Home Base/Combat UI consistent.
---

# Scavenger — UI design (hover, cursors, click affordances)

## When to apply this skill

- You are designing or modifying **UITK panels** (Main Menu, Home Base, combat HUD) and want them to feel consistent.
- The user reports that **hover cursors are not changing** over UI Toolkit elements.
- You are adding **new clickable or draggable elements** (buttons, drag handles, list rows, inventory tiles).
- You see Unity console errors like **“Invalid texture used for cursor”** or **“texture was not CPU accessible”** when setting a cursor.
- You need to decide between **USS `cursor: url(...)`**, **`UnityEngine.Cursor.SetCursor`**, and **`StyleCursor` on `VisualElement.style.cursor`**.

## UI interaction conventions (Scavenger)

### Cursors

- **Primary click targets** (main menu buttons, Home Base actions, confirm buttons):
  - Cursor: **`hand_point`**.
- **Draggable items / inventory tiles / list rows**:
  - Hover cursor: **`hand_point`** (same as clickable).
  - Drag feedback: rely on the **drag ghost + movement**; a dedicated “closed hand” cursor is optional.
- **Drag handles / movable panels** (e.g. floating panel headers):
  - Cursor: **`gauntlet_open`** (open hand / grab), matching the sci‑fi gauntlet feel.
- **Disabled / non-interactive** elements:
  - Cursor: default arrow (no hand); no cursor or color change on hover.

Rule: **one cursor owner at a time**. Combat hardware cursors must clear when the pointer enters blocking UITK UI; UITK should not fight `Cursor.SetCursor` every frame.

### Hover feedback and click affordances

- **Primary buttons**:
  - Hover: slightly brighter background and clearer border (accent color), with a short transition (~0.15–0.2s).
  - Text: remains full-contrast and legible.
- **Secondary clickable controls** (options toggles, sliders, smaller icon buttons):
  - Hover: text shifts from muted to normal text color; optional faint border or underline.
- **Lists / inventory tiles / roster rows**:
  - Hover: subtle background or border change on the **visible element**.
  - **Hitbox matches the visual** — don’t pad beyond the drawn tile/row; what you see is what you can click/drag.
- **Focus vs hover**:
  - Keyboard/gamepad focus can use a stronger outline; hover uses softer color/border changes so both can coexist without confusing the player.

## Cursor sources in Scavenger

- **Hardware cursors in combat** — `CombatHardwareCursor` uses `UnityEngine.Cursor.SetCursor` with textures from `Resources/ScavengerCursors/*` (boot, sword, target, etc.) based on turn state and tile hover.
- **UI Toolkit cursors** — Panels like Main Menu, Home Base, and Combat UI use:
  - USS rules such as `cursor: url("Assets/Art/cursors/hand_point.png") 4 4;` for simple hover states.
  - Runtime `StyleCursor` assignments (e.g. Main Menu) for more robust, scene-independent hover cursors.

## Import settings required for UITK cursor textures

When setting a cursor from C# for UITK, Unity requires:

- **Texture type**: `Cursor` (in `.meta`, `textureType: 7`, `spriteMode: 0`).
- **Readable**: `isReadable: 1` (CPU-accessible).
- **Format / mips**: RGBA32, **no mip chain** (Unity’s cursor error text: “must be RGBA32, readable, have alphaIsTransparency enabled and have no mip chain”).

If wrong: Unity logs an error and keeps the old cursor (UITK looks like it “did nothing”).

### How to fix, step-by-step

1. **Locate the runtime cursor texture**:
   - For combat + menu hover: look in `Assets/Resources/ScavengerCursors/` (e.g. `hand_point.png`, `boot.png`, `tool_sword_a.png`, `target_round_a.png`).
   - For USS-only cursors: `Assets/Art/cursors/*.png` are fine when only referenced in USS and not set via C#.
2. **Make sure the `Resources/ScavengerCursors/*` texture you use for UITK is configured like the working ones**:
   - Compare to a working cursor `.meta` (e.g. `boot.png.meta`).
   - Adjust fields so they match:
     - `isReadable: 1`
     - `spriteMode: 0`
     - `textureType: 7`
3. **Apply via UITK API, not a raw texture**:
   - Load once with `Resources.Load<Texture2D>("ScavengerCursors/hand_point")`.
   - Wrap in a `UnityEngine.UIElements.Cursor` and assign a `StyleCursor`:
   - Example pattern (don’t duplicate this into every script; reuse existing helpers where they appear):

```csharp
var tex = Resources.Load<Texture2D>("ScavengerCursors/hand_point");
if (tex != null)
{
    var uieCursor = new UnityEngine.UIElements.Cursor
    {
        texture = tex,
        hotspot = new Vector2(4f, 4f),
    };
    element.style.cursor = new StyleCursor(uieCursor);
}
```

## Choosing between USS and C# cursors

- **Use USS `cursor: url(...)`** when:
  - The cursor is purely a **styling detail** and does not need to change dynamically based on logic.
  - You can live with platform nuances and don’t need runtime checks or sharing with `Resources/ScavengerCursors`.
- **Use `StyleCursor` / `Cursor.SetCursor`** when:
  - You need the same cursor asset in both **gameplay (combat)** and **UITK overlays**.
  - You want robust error reporting (console errors) and explicit control over when the cursor changes.
  - You are already using `Resources/ScavengerCursors/*` from combat and want to match that look.

In Scavenger, prefer:

- **Combat** — continue using `CombatHardwareCursor` + `Resources/ScavengerCursors/*`.
- **Main Menu / base overlays** — use **runtime `StyleCursor`** with the same `ScavengerCursors/*` textures for hover states on key buttons and panels, with import settings verified as above.

## Debugging checklist for “UITK cursors not working”

When a user reports that hovering UITK elements does not change the cursor:

1. **Check pointer events**:
   - Confirm the element is getting `PointerEnterEvent` / `PointerLeaveEvent` (no overlay eating them).
2. **Check console for cursor import errors**:
   - Look specifically for:
     - “Invalid texture used for cursor…”
     - “texture was not CPU accessible…”
3. **Verify import settings for the texture you are using in C#**:
   - Texture lives under `Resources/ScavengerCursors/`.
   - `.meta` has:
     - `textureType: 7`
     - `spriteMode: 0`
     - `isReadable: 1`
4. **Verify you’re using `UnityEngine.UIElements.Cursor` + `StyleCursor`**:
   - Don’t try to pass a sprite; always pass the **Texture2D**.
5. **Avoid fighting `CombatHardwareCursor`**:
   - For main menu / base, there is no `CombatHardwareCursor`.
   - In combat, ensure `UIClickBlocker` / blocking panels are allowed to **clear** hardware cursors before UITK cursors take over.

If all of the above are correct and cursors still do not change, capture the exact console messages and adjust import settings or hover bindings for that panel.

