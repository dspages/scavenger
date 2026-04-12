# manifest-combat-grid-spatial

## Owns

- Tile topology, pathfinding, reachability, range geometry, LOS, vision, and spatial queries.

## Scope

- `Assets/Scripts/CombatScene/TileManager.cs`
- `Assets/Scripts/CombatScene/Tile.cs`
- `Assets/Scripts/CombatScene/helpers/PathfindingSystem.cs`
- `Assets/Scripts/CombatScene/helpers/ReachabilityResolver.cs`
- `Assets/Scripts/CombatScene/LineOfSightUtils.cs`
- `Assets/Scripts/CombatScene/VisionSystem.cs`
- `Assets/Scripts/CombatScene/AttackPreviewHelper.cs`
- `Assets/Scripts/CombatScene/IlluminationSource.cs`

## Out of scope

- Turn order, AP/cooldown decisions, enemy policy, formula tuning.

## Anchoring tests

- `Assets/Tests/Editor/LineOfSightAndRangeTests.cs`

## Handoff

- To `combat-flow-ai` when issue is orchestration/state flow.
- To `combat-rules-economy` when issue is formula/cost, not geometry.
