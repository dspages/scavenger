# manifest-save-persistence

## Owns

- Save/load DTO schema, checkpoint boundaries, backward compatibility of persisted state.

## Scope

- `Assets/Scripts/GameState/SaveData.cs`
- `Assets/Scripts/GameState/SaveManager.cs`
- `Assets/Scripts/GameState/PlayerParty.cs`
- `Assets/Scripts/GameState/EnemyParty.cs`
- `Assets/Scripts/GameState/CampaignMeta.cs`

## Out of scope

- Runtime-only combat flow unless it affects persisted fields.

## Anchoring tests

- `Assets/Tests/Editor/SaveDataTests.cs`
- `Assets/Tests/Editor/PlayerPartyExcursionTests.cs`

## Handoff

- To `content-catalogs` when new saved fields depend on registry ids.
- To `ui-panels-inventory` when persistence requires UI wiring only.
