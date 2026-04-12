# manifest-ui-panels-inventory

## Owns

- UI Toolkit combat/home-base panel behavior, inventory drag/transfer UI, tooltip composition.

## Scope

- `Assets/Scripts/UIPanels/CombatPanelUI.cs`
- `Assets/Scripts/UIPanels/HomeBaseUI.cs`
- `Assets/Scripts/UIPanels/HomeBaseLoadoutPresenter.cs`
- `Assets/Scripts/UIPanels/DerivedStatsView.cs`
- `Assets/Scripts/UIPanels/TooltipTextBuilder.cs`
- `Assets/Scripts/UIPanels/Inventory/`
- `Assets/UI/UXML/`

## Out of scope

- Authoritative combat formulas or save schema decisions.

## Anchoring tests

- `Assets/Tests/Editor/TooltipTextBuilderTests.cs`

## Handoff

- To `combat-rules-economy` when UI needs new computed values/rules.
- To `save-persistence` when panel state must be persisted.
