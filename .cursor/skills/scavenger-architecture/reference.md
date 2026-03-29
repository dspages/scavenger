# Scavenger — architecture (detail)

Companion to [`SKILL.md`](SKILL.md). Read when the map is not enough.

## Four-layer architecture

The codebase is organized so **game-rule content** can change often (items, abilities, classes, effects) without entangling Unity lifecycle, and so **rules** stay testable and **state** stays saveable.

| Layer | Role | Typical locations |
| --- | --- | --- |
| **1. Content** | Definitions only: ids, numbers, enums, archetypes. No combat loop, no MonoBehaviours. Easy to extend like “config files.” | [`Assets/Scripts/RPG/Content/`](../../../Assets/Scripts/RPG/Content/) — [`ItemCatalog`](../../../Assets/Scripts/RPG/Content/ItemCatalog.cs), [`AbilityCatalog`](../../../Assets/Scripts/RPG/Content/AbilityCatalog.cs), [`ContentRegistry`](../../../Assets/Scripts/RPG/Content/ContentRegistry.cs) |
| **2. Rules & game state** | **Rules:** pure or mostly-pure C# that applies content to situations (damage, hit chance, saves). **State:** serializable snapshots of the run (`CharacterSheet`, party, stash). Controllers and UI read this layer; they should not duplicate rule math. | [`CharacterSheet`](../../../Assets/Scripts/RPG/CharacterSheet.cs), [`AttackResolver`](../../../Assets/Scripts/RPG/AttackResolver.cs), [`HitCalculator`](../../../Assets/Scripts/RPG/HitCalculator.cs), [`RpgCombatBalance`](../../../Assets/Scripts/RPG/RpgCombatBalance.cs) (tuning constants), [`PlayerParty`](../../../Assets/Scripts/GameState/PlayerParty.cs), [`SaveData`](../../../Assets/Scripts/GameState/SaveData.cs) |
| **3. Controllers (Unity runtime)** | Per-avatar and scene systems: input, grid movement, turn participation, spawning visuals. They **orchestrate** actions and forward to rules; they do not own long-term definitions of damage or item stats. | [`CombatController`](../../../Assets/Scripts/CombatScene/Controllers/CombatController.cs), [`PlayerController`](../../../Assets/Scripts/CombatScene/Controllers/PlayerController.cs), [`EnemyController`](../../../Assets/Scripts/CombatScene/Controllers/EnemyController.cs), [`TurnManager`](../../../Assets/Scripts/CombatScene/TurnManager.cs), [`AvatarController`](../../../Assets/Scripts/CombatScene/Controllers/AvatarController.cs) |
| **4. UI / presentation** | UXML/USS, panels, tooltips, combat log, victory banners. Subscribes to or polls model/state; **does not** implement core combat formulas. | [`CombatPanelUI`](../../../Assets/Scripts/UIPanels/CombatPanelUI.cs), [`HomeBaseUI`](../../../Assets/Scripts/UIPanels/HomeBaseUI.cs), [`DerivedStatsView`](../../../Assets/Scripts/UIPanels/DerivedStatsView.cs), [`Assets/UI/UXML/`](../../../Assets/UI/UXML/) |

**Why four layers:** Separates *what the game is* (content) from *what happens when you attack* (rules), from *who is on the board this frame* (controllers), from *what the player sees* (UI). That keeps expansion content in catalogs, keeps balance logic in one place (resolvers + tuning), and keeps saves stable (state DTOs + ids).

## Architectural principles (summary)

- **Content is data-driven** — Prefer adding or editing catalog entries and registry ids over hardcoding new subclasses for every weapon or ability.
- **Dependency direction flows inward** — UI and controllers depend on rules, state, and content. Content catalogs and pure rules code must not depend on scene-specific controllers or presentation. (Shared value types and enums, e.g. on items, are fine.)
- **Rules, not controllers, own outcomes** — Hit chance, damage, crit, armor reduction, and similar belong in resolvers / `CharacterSheet` (or dedicated pure helpers), not in `CombatController` or UI. Controllers choose targets and fire actions; resolvers apply rules.
- **Single source of truth for combat numbers** — The same pipeline (content + resolvers + tuning helpers) should drive both gameplay and honest UI previews. Do not maintain parallel “display-only” damage or hit values that can drift from actual resolution.
- **Tuning lives in tuning homes** — Balance and combat-tuning constants belong in dedicated places (e.g. [`RpgCombatBalance`](../../../Assets/Scripts/RPG/RpgCombatBalance.cs), resolvers, catalogs). Do not mix ad hoc “balance patch” constants into random scripts such as UI panels or controllers; that hides where the real rules live and makes regressions likely.
- **`CharacterSheet` vs avatar** — Saves and rules center on [`CharacterSheet`](../../../Assets/Scripts/RPG/CharacterSheet.cs). `GameObject` / [`CombatController`](../../../Assets/Scripts/CombatScene/Controllers/CombatController.cs) / [`AvatarController`](../../../Assets/Scripts/CombatScene/Controllers/AvatarController.cs) bridge to Unity (visuals, input, lifecycle). Logic that can stay pure should not require a live scene object.
- **Player and AI parity** — Prefer the same `Action` types and the same resolution path for PCs and enemies so AI does not fork a second combat model. [`EnemyController`](../../../Assets/Scripts/CombatScene/Controllers/EnemyController.cs) picks targets and actions; it should not reimplement hit or damage math.
- **Observation over coupling** — UI and logs react via events, explicit refresh hooks, or small facades (`OnHealthChanged`, `CombatLog`, etc.). Avoid deep reaches from UI into combat internals when a subscription or callback would do.
- **Game state is serializable** — Anything that must survive between sessions or scenes belongs in party/save types with stable ids; see [`scavenger-data-persistence`](../scavenger-data-persistence/SKILL.md).
- **Combat vs campaign scope** — Persistent party and stash live in [`PlayerParty`](../../../Assets/Scripts/GameState/PlayerParty.cs); grid positions and turn order are combat-scene concerns. Do not blur the two without going through an explicit checkpoint or save boundary.
- **MonoBehaviours orchestrate** — `Action` components handle timing, movement, and VFX hooks; they call into rules with `AttackContext` / similar and apply results (e.g. log, popups) without re-deriving formulas in three places.
- **UI is a view** — Panels display `CharacterSheet` and listen for changes; they do not become the authority for stats or inventory rules.
- **Avoid duplicate systems** — One turn pipeline, one content registry, one save format evolution path; extend rather than parallel copies.

## Related skills (cross-cutting)

Use the architecture skill for *where things live*; use these for *how to change or verify them* without duplicating long prose here:

| Topic | Skill |
| --- | --- |
| Save ids, JsonUtility, checkpoints, extending `SaveData` | [`scavenger-data-persistence`](../scavenger-data-persistence/SKILL.md) |
| Root cause vs symptom, intended codepath, avoiding band-aids | [`scavenger-debugging-workflow`](../scavenger-debugging-workflow/SKILL.md) |
| UXML/USS, panels, drag/transfer, shared HUD chrome | [`scavenger-ui-toolkit`](../scavenger-ui-toolkit/SKILL.md) |
| EditMode tests, Unity Console, safe test runs | [`scavenger-post-change-verification`](../scavenger-post-change-verification/SKILL.md) |
| Large refactors, player guide, when to touch multiple docs | [`scavenger-change-companion`](../scavenger-change-companion/SKILL.md) |

## Major objects — intended roles

### `CharacterSheet`

- Holds **identity and primary statistics** (e.g. strength, agility, level, class) and **current resources** (health, mana, action points in combat).
- **Derived combat values** (effective armor, weapon summaries, resistances, displayed damage bands) are computed for display and for rules that need them — see [`DerivedStatsView`](../../../Assets/Scripts/UIPanels/DerivedStatsView.cs) for how the UI presents compact vs full character views.
- Delegates equipment rules to [`Equipment`](../../../Assets/Scripts/RPG/Inventory/Equipment.cs); remains the hub for “what this creature is” in RPG terms, without Unity-specific lifecycle (avatar wiring lives on controllers).

### Inventory UI and character examination (shared across scenes)

- **Combat** — [`CombatPanelUI`](../../../Assets/Scripts/UIPanels/CombatPanelUI.cs) builds from [`CombatPanel.uxml`](../../../Assets/UI/UXML/CombatPanel.uxml): action bar, inventory panel, equipment slots, and optional stats block.
- **Home base / town** — [`HomeBaseUI`](../../../Assets/Scripts/UIPanels/HomeBaseUI.cs) reuses the **same inventory panel subtree** (typically cloned from `CombatPanel.uxml`) so loadout and stash flows match combat. [`HomeBaseLoadoutPresenter`](../../../Assets/Scripts/UIPanels/HomeBaseLoadoutPresenter.cs) wires roster selection to the shared inventory UI pattern.
- **Principle:** One mental model for “inspect character + drag items” whether you are in **CombatScene** or **HomeBase**; scene-specific chrome differs, but the character sheet + inventory behavior should stay consistent.

### `Action` (and subclasses)

- **`Action`** — `MonoBehaviour` on a combatant’s avatar, representing **one selectable thing** the unit can do (move, melee, ranged, ground AoE, kick, abilities).
- Handles **phase machine** relevant to that action: pathing, `AttackSequence` coroutines, when projectiles fire, when `PerformAttack` runs.
- **Does not** own the authoritative damage/hit formula — it builds context (weapon, ability data, distance) and calls [`AttackResolver`](../../../Assets/Scripts/RPG/AttackResolver.cs) / related pure logic.
- Subtypes ([`ActionMeleeAttack`](../../../Assets/Scripts/CombatScene/Actions/ActionMeleeAttack.cs), [`ActionRangedAttack`](../../../Assets/Scripts/CombatScene/Actions/ActionRangedAttack.cs), [`ActionGroundAttack`](../../../Assets/Scripts/CombatScene/Actions/ActionGroundAttack.cs), etc.) specialize targeting and presentation only.

### Controllers

| Type | Role |
| --- | --- |
| [`CombatController`](../../../Assets/Scripts/CombatScene/Controllers/CombatController.cs) | Base for any unit on the grid: links to `CharacterSheet`, tile occupancy, action selection, selectable tiles, turn flags. Subclasses specialize input vs AI. |
| [`PlayerController`](../../../Assets/Scripts/CombatScene/Controllers/PlayerController.cs) | Human input: hover, clicks, HUD-driven action selection. |
| [`EnemyController`](../../../Assets/Scripts/CombatScene/Controllers/EnemyController.cs) | AI: candidate actions, target picking. |
| [`AvatarController`](../../../Assets/Scripts/CombatScene/Controllers/AvatarController.cs) | Avatar lifecycle: attach `Action` components from known abilities, visuals, popups tied to the `GameObject`. |
| [`TurnManager`](../../../Assets/Scripts/CombatScene/TurnManager.cs) | Global combat turn order, victory/defeat when one side has no living units, coordination with UI for banners. |

**Principle:** Controllers sit in **layer 3**; they connect Unity to **layer 2** rules and **layer 1** content references (e.g. ability ids → `AbilityData`).

## Scene flow (typical)

1. **Main menu** — New game resets [`PlayerParty`](../../../Assets/Scripts/GameState/PlayerParty.cs) and [`CampaignMeta`](../../../Assets/Scripts/GameState/CampaignMeta.cs), then loads **HomeBase**. Load game (when implemented) should restore state then enter an appropriate scene (usually Home Base).
2. **Home base** — Party roster, [`PlayerParty.sharedStash`](../../../Assets/Scripts/GameState/PlayerParty.cs), excursion squad selection, launching into **CombatScene** when a mission starts.
3. **Combat** — [`TurnManager`](../../../Assets/Scripts/CombatScene/TurnManager.cs) drives turns; [`CombatController`](../../../Assets/Scripts/CombatScene/Controllers/CombatController.cs) hierarchy for player vs enemies; victory/defeat via [`CombatPanelUI`](../../../Assets/Scripts/UIPanels/CombatPanelUI.cs). Returning to home base is expected to advance campaign time via [`CampaignMeta.AdvanceWeekAfterMission`](../../../Assets/Scripts/GameState/CampaignMeta.cs) when that flow is wired.

Exact transition code may evolve; treat this as **intent** so new code does not invent a parallel flow.

## Where state lives

- **Party characters** — [`PlayerParty.partyMembers`](../../../Assets/Scripts/GameState/PlayerParty.cs) (`CharacterSheet` instances).
- **Camp stash** — [`PlayerParty.sharedStash`](../../../Assets/Scripts/GameState/PlayerParty.cs) (shared inventory).
- **Excursion squad** — [`PlayerParty.excursionSquadIndices`](../../../Assets/Scripts/GameState/PlayerParty.cs) (who deploys next).
- **Campaign clock / gold** — [`CampaignMeta`](../../../Assets/Scripts/GameState/CampaignMeta.cs) (lightweight until a fuller meta layer exists).

Combat-specific state (positions, turn order, map) lives in **CombatScene** objects and is **not** the same as the persistent party snapshot; see [`scavenger-data-persistence`](../scavenger-data-persistence/SKILL.md) for checkpoint rules.

## Combat stack (orientation)

- **Turn loop** — [`TurnManager`](../../../Assets/Scripts/CombatScene/TurnManager.cs).
- **Per-unit behavior** — [`CombatController`](../../../Assets/Scripts/CombatScene/Controllers/CombatController.cs), [`PlayerController`](../../../Assets/Scripts/CombatScene/Controllers/PlayerController.cs), [`EnemyController`](../../../Assets/Scripts/CombatScene/Controllers/EnemyController.cs).
- **Actions** — [`Assets/Scripts/CombatScene/Actions/`](../../../Assets/Scripts/CombatScene/Actions/) (`Action` subclasses).
- **Grid / tiles** — [`TileManager`](../../../Assets/Scripts/CombatScene/TileManager.cs), [`Tile`](../../../Assets/Scripts/CombatScene/Tile.cs).

Avoid introducing a **second** global combat state machine; extend the existing turn/action pipeline.

## Content (items, abilities)

- Catalogs and registries under [`Assets/Scripts/RPG/Content/`](../../../Assets/Scripts/RPG/Content/) (e.g. [`ItemCatalog`](../../../Assets/Scripts/RPG/Content/ItemCatalog.cs), [`AbilityCatalog`](../../../Assets/Scripts/RPG/Content/AbilityCatalog.cs)).
- Saved games reference content by **ids** (e.g. ability ids, item registry ids); changing ids breaks loads — see persistence skill.

## Changing one subsystem — watch these

| Change | Also consider |
| --- | --- |
| `CharacterSheet` fields | Save/load ([`CharacterSaveData`](../../../Assets/Scripts/GameState/SaveData.cs)), UI that displays stats |
| New equipment or item types | `ItemSaveData` / registry resolution in [`SaveData.cs`](../../../Assets/Scripts/GameState/SaveData.cs), catalogs |
| New persistent meta | [`GameSaveData`](../../../Assets/Scripts/GameState/SaveData.cs), possibly [`CampaignMeta`](../../../Assets/Scripts/GameState/CampaignMeta.cs) |
| Scene load order | [`SceneNames`](../../../Assets/Scripts/GameState/SceneNames.cs), bootstrap that calls [`PlayerParty.EnsureInitialized`](../../../Assets/Scripts/GameState/PlayerParty.cs) |
