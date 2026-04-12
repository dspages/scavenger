# manifest-content-catalogs

## Owns

- Catalog definitions, registry id wiring, and data-driven content setup.

## Scope

- `Assets/Scripts/RPG/Content/ContentRegistry.cs`
- `Assets/Scripts/RPG/Content/ItemCatalog.cs`
- `Assets/Scripts/RPG/Content/AbilityCatalog.cs`
- `Assets/Scripts/RPG/Content/ItemData.cs`
- `Assets/Scripts/RPG/Content/AbilityData.cs`
- `Assets/Scripts/RPG/Content/WeaponData.cs`
- `Assets/Scripts/RPG/SpeciesRules.cs`

## Out of scope

- Turn flow logic and panel behavior unless required by new data fields.

## Anchoring tests

- `Assets/Tests/Editor/ContentRegistryTests.cs`
- `Assets/Tests/Editor/StatusEffectDataTests.cs`

## Handoff

- To `save-persistence` when id changes affect saved data.
- To `combat-rules-economy` when new data needs formula/cost rule support.
