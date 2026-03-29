---
name: scavenger-architecture
description: >-
  High-level map of the Scavenger Unity project: scenes, script folders, four-layer
  architecture (content, rules & state, controllers, UI), dependency direction,
  tuning vs presentation, and anchor types for navigation. Use when orienting in
  the codebase, planning where a feature belongs, or avoiding duplicate systems.
  For subsystem detail, layered principles, cross-links to other skills, and
  major-object roles, read reference.md in the same folder.
---

# Scavenger — architecture (map)

Read **[`reference.md`](reference.md)** for the **four-layer architecture**, expanded principles (dependency direction, single source of truth for combat numbers, `CharacterSheet` vs avatar, player/AI parity, observation vs coupling, tuning homes vs UI/controllers), a **related skills** table, roles of major types (`CharacterSheet`, `Action`, controllers, shared character/inventory UI), scene flows, and “if you change X, consider Y.”

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
