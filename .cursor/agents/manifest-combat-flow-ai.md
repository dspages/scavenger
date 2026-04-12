# manifest-combat-flow-ai

## Owns

- Turn lifecycle, action selection/orchestration, controller flow, and enemy action policy.

## Scope

- `Assets/Scripts/CombatScene/TurnManager.cs`
- `Assets/Scripts/CombatScene/Controllers/CombatController.cs`
- `Assets/Scripts/CombatScene/Controllers/PlayerController.cs`
- `Assets/Scripts/CombatScene/Controllers/EnemyController.cs`
- `Assets/Scripts/CombatScene/Controllers/AvatarController.cs`
- `Assets/Scripts/CombatScene/helpers/ActionValidator.cs`
- `Assets/Scripts/CombatScene/helpers/ActionSelector.cs`
- `Assets/Scripts/CombatScene/Actions/`

## Out of scope

- Tile math/LOS internals and combat formula ownership.

## Anchoring tests

- `Assets/Tests/Editor/CombatActionAffordanceTests.cs` (validation behavior)
- `Assets/Tests/PlayMode/SceneSmokeTests.cs` (runtime orchestration guardrail)

## Handoff

- To `combat-grid-spatial` for LOS/path/range math changes.
- To `combat-rules-economy` for hit/damage/cost formula changes.
