---
name: scavenger-architecture
description: >-
  Project map: scenes, key folders, layer boundaries/ownership. Use to place features,
  orient quickly, and avoid duplicate systems. Details in `reference.md`.
---

# Scavenger — architecture (map)

Read **[`reference.md`](reference.md)** for: four-layer architecture, dependency direction, ownership/state rules, major type roles, scene flows, and the ability resource contract. For the Home Base weekly command wireframe, see **[`scavenger-ui-toolkit/reference.md`](../scavenger-ui-toolkit/reference.md)**.

## Scenes

| Scene asset | `SceneNames` constant | Role |
| --- | --- | --- |
| [`Assets/Scenes/MainMenu.unity`](../../../Assets/Scenes/MainMenu.unity) | `MainMenu` | New game / load / options → [`MainMenuUI`](../../../Assets/Scripts/UIPanels/MainMenuUI.cs) |
| [`Assets/Scenes/HomeBase.unity`](../../../Assets/Scenes/HomeBase.unity) | `HomeBase` | Preparation, stash, squad, launch combat → [`HomeBaseUI`](../../../Assets/Scripts/UIPanels/HomeBaseUI.cs) |
| [`Assets/Scenes/CombatScene.unity`](../../../Assets/Scenes/CombatScene.unity) | `Combat` | Tactical combat → [`TurnManager`](../../../Assets/Scripts/CombatScene/TurnManager.cs), [`CombatPanelUI`](../../../Assets/Scripts/UIPanels/CombatPanelUI.cs) |

Constants: [`SceneNames`](../../../Assets/Scripts/GameState/SceneNames.cs).

## Top-level script areas

| Area | Path | Notes |
| --- | --- | --- |
| Game state & meta | [`Assets/Scripts/GameState/`](../../../Assets/Scripts/GameState/) | [`PlayerParty`](../../../Assets/Scripts/GameState/PlayerParty.cs), [`CampaignMeta`](../../../Assets/Scripts/GameState/CampaignMeta.cs), save types ([`SaveManager`](../../../Assets/Scripts/GameState/SaveManager.cs), [`GameSaveData`](../../../Assets/Scripts/GameState/SaveData.cs)) |
| Combat runtime | [`Assets/Scripts/CombatScene/`](../../../Assets/Scripts/CombatScene/) | Grid, turns, controllers, actions, vision |
| RPG model & content | [`Assets/Scripts/RPG/`](../../../Assets/Scripts/RPG/) | [`CharacterSheet`](../../../Assets/Scripts/RPG/CharacterSheet.cs), combat resolution, [`RPG/Content/`](../../../Assets/Scripts/RPG/Content/) catalogs |
| UI (UI Toolkit) | [`Assets/Scripts/UIPanels/`](../../../Assets/Scripts/UIPanels/) | Menu, combat panel, home base, tooltips, inventory UI |

## UI assets

- UXML/USS: [`Assets/UI/UXML/`](../../../Assets/UI/UXML/) — see [`scavenger-ui-toolkit`](../scavenger-ui-toolkit/SKILL.md).

## Related skills

See **[`reference.md`](reference.md) → Related skills (cross-cutting)** for the full table (persistence, debugging, UI toolkit, post-change verification, change companion).

- Persistence and checkpoints: [`scavenger-data-persistence`](../scavenger-data-persistence/SKILL.md)
- UI structure: [`scavenger-ui-toolkit`](../scavenger-ui-toolkit/SKILL.md)
