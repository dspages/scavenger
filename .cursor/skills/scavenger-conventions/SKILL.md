---
name: scavenger-conventions
description: >-
  Project formatting, naming, tooling, and directory layout for Scavenger: where new scripts,
  assets, and tests go, and what top-level folders are for. Use when adding files, choosing
  namespaces or test placement, or matching existing style. Does not cover PR/branch workflow.
  For implementation habits (DRY, pitfalls), read scavenger-coding-principles; for system
  design and flows, read scavenger-architecture.
---

# Scavenger — project conventions

**Where things go and how the repo is organized.** **How to write** implementation code (DRY, pitfalls) is **[`scavenger-coding-principles`](../scavenger-coding-principles/SKILL.md)**. **Scenes, layers, ownership, and gameplay flows** are **[`scavenger-architecture`](../scavenger-architecture/SKILL.md)** and its [`reference.md`](../scavenger-architecture/reference.md).

## Formatting

- Match **style of neighboring files** in the same directory (indentation, brace placement, blank lines).
- Prefer **Unix line endings** (`LF`) where the editor allows; avoid mixing line endings in one file.
- There is **no** committed `.editorconfig` at the repo root yet; IDEs use defaults. The team may add one later for shared C# rules—if present, follow it.

## Naming

- **Types and methods:** `PascalCase` (e.g. `CharacterSheet`, `HasLineOfSight`).
- **Locals and parameters:** `camelCase`.
- **Private fields:** match existing files in the folder (many use `camelCase` without underscore; stay consistent with siblings).
- **Assets:** use clear, stable names; Unity generates `.meta` files—do not hand-edit GUIDs; rename assets in the Editor when possible so references update.

## Tooling

- **Unity version** is defined by the project (`ProjectSettings/ProjectVersion.txt`); open the project with the matching Editor.
- **Tests** run from the Unity Test Runner (`Window > General > Test Runner`). EditMode vs PlayMode and MCP-assisted runs are detailed in [`scavenger-post-change-verification`](../scavenger-post-change-verification/SKILL.md).
- **Assemblies:** game code uses the default **`Assembly-CSharp`** (no `*.asmdef` under [`Assets/`](../../../Assets/) at present). If asmdefs are introduced later, place them deliberately and update this skill.

## Directory structure (purpose and where new files go)

Top-level under [`Assets/`](../../../Assets/):

| Area | Path | Purpose |
| --- | --- | --- |
| **Scripts** | [`Assets/Scripts/`](../../../Assets/Scripts/) | Runtime and shared game code—see table below. |
| **UI** | [`Assets/UI/`](../../../Assets/UI/) (e.g. [`UXML/`](../../../Assets/UI/UXML/)) | UI Toolkit **UXML/USS**; pair with scripts under `UIPanels` / UI-related code. |
| **Scenes** | [`Assets/Scenes/`](../../../Assets/Scenes/) | Loaded scenes (`MainMenu`, `HomeBase`, `Combat`, etc.). |
| **Tests** | [`Assets/Tests/`](../../../Assets/Tests/) | Automated tests—see Tests below. |
| **Resources** | [`Assets/Resources/`](../../../Assets/Resources/) | `Resources.Load` assets only; keep small and intentional. |
| **Art** | [`Assets/Art/`](../../../Assets/Art/) | Art sources (sprites, cursors, etc.). |
| **Prefabs** | [`Assets/Prefabs/`](../../../Assets/Prefabs/) | Prefab assets. |
| **Editor** | [`Assets/Editor/`](../../../Assets/Editor/) | Editor-only scripts and tools. |
| **Other** | `SpriteShapes`, `TextMesh Pro`, `UI Toolkit`, etc. | Third-party or package-aligned content; avoid mixing bespoke game scripts there unless the package pattern requires it. |

### `Assets/Scripts/` (game code)

| Path | Purpose |
| --- | --- |
| [`GameState/`](../../../Assets/Scripts/GameState/) | Run-wide state, saves, campaign meta, scene names. |
| [`CombatScene/`](../../../Assets/Scripts/CombatScene/) | Combat grid, turns, actions, vision, controllers, combat UI helpers. |
| [`RPG/`](../../../Assets/Scripts/RPG/) | Character model, combat resolution, content catalogs, inventory types. |
| [`UIPanels/`](../../../Assets/Scripts/UIPanels/) | UI Toolkit document controllers, panels, shared inventory UI. |

Place new code next to **the feature** it belongs to (e.g. combat rules under `CombatScene/` or `RPG/` per existing patterns). When unsure, read [`scavenger-architecture`](../scavenger-architecture/SKILL.md).

### Tests

| Kind | Location |
| --- | --- |
| **EditMode** (pure logic, fast) | [`Assets/Tests/Editor/`](../../../Assets/Tests/Editor/) |
| **PlayMode** (runtime / scenes / frames) | [`Assets/Tests/PlayMode/`](../../../Assets/Tests/PlayMode/) |
| **Committed test scenes** (optional) | [`Assets/Tests/Scenes/`](../../../Assets/Tests/Scenes/) |

Mirror namespaces and areas under test to the code under [`Assets/Scripts/`](../../../Assets/Scripts/) where it helps navigation.

### Docs

- Player-facing: [`docs/PLAYER_GUIDE.md`](../../../docs/PLAYER_GUIDE.md) (see [`scavenger-change-companion`](../scavenger-change-companion/SKILL.md)).
- Agent index: [`AGENTS.md`](../../../AGENTS.md) at repo root.

## Related

- Coding principles: [`scavenger-coding-principles`](../scavenger-coding-principles/SKILL.md)
- Architecture: [`scavenger-architecture`](../scavenger-architecture/SKILL.md)
