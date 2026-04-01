---
name: scavenger-data-persistence
description: >-
  Save/checkpoint design: `JsonUtility` DTO rules, save types, item/ability ids, and
  schema-change constraints. Use when changing anything that must survive between sessions.
---

# Scavenger — data persistence and checkpoints

## Design: checkpoints only (between combat)

**Saving and loading are intended only at checkpoints** — i.e. between combat missions, not during active combat. That keeps persistence off the hot combat path and avoids serializing tactical state (grid, turn order, partial actions).

- Implement **Save** / **Load** entry points when leaving or entering **non-combat** scenes (e.g. Home Base, Main Menu), not from [`CombatScene`](../../../Assets/Scenes/CombatScene.unity) gameplay.
- Combat outcomes should **commit** back to the persistent model (party health, inventory, dead flags, meta) **after** combat ends and before the next checkpoint, as flows are wired.

## Core API

| Piece | Role |
| --- | --- |
| [`SaveManager`](../../../Assets/Scripts/GameState/SaveManager.cs) | Writes/reads `savegame.json` under `Application.persistentDataPath` |
| [`GameSaveData`](../../../Assets/Scripts/GameState/SaveData.cs) | Top-level snapshot; `FromCurrentState()` / `RestoreState()` |
| [`CharacterSaveData`](../../../Assets/Scripts/GameState/SaveData.cs) | Per-character stats, inventory slots, equipment, `abilityIds` |
| [`ItemSaveData`](../../../Assets/Scripts/GameState/SaveData.cs) | Stack + optional `registryId` for catalog-backed items |

## JsonUtility rules

- Types must be shaped for **`JsonUtility`**: `[Serializable]`, public fields (or `[SerializeField]` on MonoBehaviours — save data is plain classes).
- **Renaming or removing** serialized fields without migration can invalidate old saves; treat as a schema change.
- **Polymorphism** is limited: `JsonUtility` does not round-trip arbitrary subclass graphs; item data uses a flat `ItemSaveData` with flags and `registryId` where possible.

## Items and abilities

- Items often resolve through **[`ItemCatalog`](../../../Assets/Scripts/RPG/Content/ItemCatalog.cs)** via `registryId` in `ItemSaveData`; fallback fields exist when registry lookup is insufficient.
- Abilities are stored as **`abilityIds`** (strings) on [`CharacterSaveData`](../../../Assets/Scripts/GameState/SaveData.cs); keep ids stable or provide migration.

## What the current snapshot includes

[`GameSaveData`](../../../Assets/Scripts/GameState/SaveData.cs) currently persists **party members** via `CharacterSaveData`. **Camp stash**, **excursion squad**, and **CampaignMeta** may need to be added to the save model as those flows harden — extend `GameSaveData` deliberately and document new fields.

### Weekly roster & assignment hooks (when implemented)

- **`CanAssign(character, slot)`** — not persisted; only **assignment outcomes** (who is in which slot / job) belong in save data. When weekly UI lands, ensure **JsonUtility** DTOs can round-trip every valid state the UI allows after `CanAssign` checks.
- **Excursion squad** — mirror [`PlayerParty.excursionSquadIndices`](../../../Assets/Scripts/GameState/PlayerParty.cs) (or successor) in save data when loads must round-trip Home Base → combat.
- **Per-character weekly base jobs** — when the meta layer stores “who is on workbench / barracks / …”, add a stable representation (indices + room/task ids) to `GameSaveData` / per-character blobs; **JsonUtility** shape rules apply.
- **Shared resources** — **mana crystals**, **tech components**, **sanity/morale** (`currentSanity` on [`CharacterSheet`](../../../Assets/Scripts/RPG/CharacterSheet.cs)), and related stacks must stay consistent with combat + workbench (see [`scavenger-architecture`](../scavenger-architecture/reference.md) ability contract).

## Options vs game save

[`TooltipDetailSettings`](../../../Assets/Scripts/GameState/TooltipDetailSettings.cs) uses **PlayerPrefs** for UI preferences — not the same as `savegame.json`. Do not conflate the two.

## Related

- High-level state ownership: [`scavenger-architecture`](../scavenger-architecture/SKILL.md) and [`reference.md`](../scavenger-architecture/reference.md).
