---
name: scavenger-ui-toolkit
description: >-
  Unity UI Toolkit patterns for Scavenger: floating vs docked panels, shared chrome
  and USS classes, drag handles and PanelDragController, and two-sided inventory
  transfer UI. Use when adding or changing UXML/USS, combat or home-base panels,
  floating tooltips, or stash/chest/corpse transfer flows.
---

# Scavenger — UI Toolkit (style and structure)

For **palette, typography, spacing, and visual principles**, read **[`reference.md`](reference.md)**.

## When to apply

- New or changed **UXML/USS**, visual layout, or panel chrome.
- **Floating** popouts (combat log, popped inventory, future loot/corpse transfer) or **docked** layouts (e.g. Home Base columns).
- **Drag-and-drop** or **two-sided** inventory-style transfers.

## Where assets live

- Panel UXML and USS generally under [`Assets/UI/UXML/`](../../../Assets/UI/UXML/) (e.g. `CombatPanel.uxml`, `CombatPanel.uss`, `HomeBase.uss`, `Theme.uss`).
- Match **existing** naming, class names, and hierarchy in nearby panels before inventing new patterns.

## Floating panels

- Use a top **chrome** row: **title**, **drag handle**, optional minimize/close.
- Name new drag handles **`PanelDragHandle`** in UXML, or follow existing names such as **`CombatLogDragHandle`**.
- Implement dragging with [`PanelDragController`](../../../Assets/Scripts/UIPanels/PanelDragController.cs): call **`Attach()`** once after resolving the panel and handle.

## Docked panels

- Reuse the same **visual** chrome where it fits Home Base or similar layouts.
- Shared USS: **`game-floating-panel__header`** (compact drag strip) and **`game-floating-panel__title-row`** (taller title + actions) in [`CombatPanel.uss`](../../../Assets/UI/UXML/CombatPanel.uss).
- Dragging is **optional** for docked layouts.

## Two-sided transfer (stash, chests, corpses)

- Mirror the pattern used by [`HomeBaseLoadoutPresenter`](../../../Assets/Scripts/UIPanels/HomeBaseLoadoutPresenter.cs) and [`DragDropController`](../../../Assets/Scripts/UIPanels/Inventory/DragDropController.cs).
- Read the summary in [`InventoryTransferUiPattern`](../../../Assets/Scripts/UIPanels/InventoryTransferUiPattern.cs) for the project’s transfer UI conventions.

## Styling

- Prefer **USS classes** and shared theme rules (`Theme.uss`, panel `.uss` files) over inline styles in UXML unless the project already uses inline for that element.
- Keep spacing, typography, and color **consistent** with adjacent panels and `Theme.uss`.
