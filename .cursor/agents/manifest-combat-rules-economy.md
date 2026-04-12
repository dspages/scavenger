# manifest-combat-rules-economy

## Owns

- Combat formulas, hit/crit, damage resolution, and cost affordability/spend consistency.

## Scope

- `Assets/Scripts/RPG/AttackResolver.cs`
- `Assets/Scripts/RPG/HitCalculator.cs`
- `Assets/Scripts/RPG/AttackContext.cs`
- `Assets/Scripts/RPG/AttackResult.cs`
- `Assets/Scripts/RPG/CharacterSheet.cs`
- `Assets/Scripts/RPG/CharacterSheet.DerivedStats.cs`
- `Assets/Scripts/RPG/CombatActionAffordance.cs`
- `Assets/Scripts/RPG/CombatItemSpend.cs`
- `Assets/Scripts/RPG/RpgCombatBalance.cs`
- `Assets/Scripts/RPG/Content/` (when data values affect formula/cost behavior)

## Out of scope

- Turn order/state machine, controller input flow, panel rendering.

## Anchoring tests

- `Assets/Tests/Editor/AttackResolverTests.cs`
- `Assets/Tests/Editor/CombatActionAffordanceTests.cs`
- `Assets/Tests/Editor/CombatItemSpendTests.cs`
- `Assets/Tests/Editor/HitCalculatorTests.cs`

## Handoff

- To `combat-flow-ai` for sequencing/call-order bugs.
- To `ui-panels-inventory` when only presentation is wrong and rules are correct.
