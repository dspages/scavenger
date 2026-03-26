/// <summary>
/// Documents the intended UI pattern for loot chests, enemy corpses, and other two-sided inventory transfers.
/// </summary>
/// <remarks>
/// Reuse the same panel chrome as stash / loadout (see <see cref="PanelDragController"/> and AGENTS.md):
/// two hosts with matching headers (title, optional drag handle, minimize/close), each containing an
/// <see cref="InventoryUIManager"/> grid and/or equipment strip; wire <see cref="DragDropController"/> with
/// source/destination inventories the same way as <see cref="HomeBaseLoadoutPresenter"/> does for
/// <see cref="PlayerParty.sharedStash"/> and the selected character.
/// </remarks>
public static class InventoryTransferUiPattern
{
}
