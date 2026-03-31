# Scavenger — UI look and feel (visual design)

Companion to [`SKILL.md`](SKILL.md), which covers **structure** (panels, drag, transfer patterns). Use this file for **art direction**: palette, typography, spacing, and cohesion.

## Source of truth

- **Theme tokens** live in [`Theme.uss`](../../../Assets/UI/UXML/Theme.uss) (`:root` CSS variables): surfaces, text, accent, status colors, rarity, radii, spacing, type scale.
- Panel-specific USS files (e.g. [`CombatPanel.uss`](../../../Assets/UI/UXML/CombatPanel.uss), [`HomeBase.uss`](../../../Assets/UI/UXML/HomeBase.uss)) should **compose** theme utilities and variables rather than introducing one-off hex colors.

## Palette intent (sci‑fi scavenger)

- **Surfaces** — Dark, slightly cool neutrals (`--color-surface`, `--color-surface-alt`, `--color-surface-soft`) for panels and overlays; keep contrast high enough for long sessions.
- **Primary** — Teal (`--color-primary`) reads as salvage/tech; use for primary actions and key highlights.
- **Accent** — Amber (`--color-accent`) for selection and emphasis; use sparingly so it stays meaningful.
- **Semantic** — Danger / warning / success tokens for feedback; **damage-type** and **rarity** channels have dedicated variables — reuse them for combat and loot UI consistency.

## Layout and rhythm

- Prefer **theme spacing** (`--spacing-xs` through `--spacing-xl`) and **radius** tokens for cards, buttons, and clusters so screens feel like one system.
- **Type scale** is defined in `Theme.uss` (caption through heading); avoid arbitrary font sizes in panel USS unless matching an existing utility class pattern.

## Principles

- **One visual language** — New screens should look like they belong next to Combat and Home Base: same surfaces, same accent discipline, same density.
- **Readability first** — Muted text (`--color-text-muted`, `--color-text-soft`) for secondary copy; full `--color-text` for primary labels and body.
- **Extend the theme** — If you need a new recurring color or size, add a **variable in `Theme.uss`** and utility classes there rather than scattering literals across UXML/USS.

## Home Base — weekly command (target UX)

**Interaction contract** for the between-mission screen ([`HomeBaseUI`](../../../Assets/Scripts/UIPanels/HomeBaseUI.cs), [`HomeBase.uxml`](../../../Assets/UI/UXML/HomeBase.uxml)). **Current implementation** may still use a **horizontal roster** and per-row **Deploy** toggles; this section is the **target** end state (no UXML/UI change required to read this skill).

### Excursion squad column (right)

- **Up to four ordered slots** (spawn order preserved).
- **Empty slot — two paths:** (1) **drag** a roster row onto the slot; (2) **click** → **dropdown** of valid, unassigned heroes. Wireframe: drop target + `[ v ]`.
- **Filled slot:** character name (optional class shorthand) + **remove / bench** — `[ − ]` (or bench icon in shipping).

### Chosen pairing

- **Dropdowns** (excursion squad slots, base job pickers, any “pick a character for this slot” menu): list only heroes who are **unassigned** this week **and** pass **`CanAssign(character, slot)`** for that slot. Validity is a **hook** — **today** always `true`; future examples: workbench needs **components** in stash; excursion might require **minimum HP**. Single shared predicate for **dropdown filtering** and **drop acceptance**.
- **Roster list:** **Full party** at the base (everyone), **not** idle-only — vertical **scroll**; small **icon** beside each name for **current weekly assignment** (excursion slot, base room, unassigned, etc.); **legend** maps icons to job categories.
- **Drag-and-drop:** start from **any** roster row (assigned or not). **Valid** drop → **atomically** remove from the previous assignment (if any) and assign to the new slot **iff** `CanAssign` passes. **Invalid** drop → **no-op** (do not unassign old job); light feedback (reject cursor, brief toast).
- **Hybrid intent:** dropdown = “pick from the bench”; drag = “shuffle the board” (steal from another job). Alternatives such as greyed full lists or idle-only roster are **superseded** by this pairing unless design revisits them.

### Wireframe (ASCII)

```text
+------------------------------------------------------------------------------+
|  Week …   Gold …   Roster n / cap                         [ Confirm week ]    |
+------------------------------------------------------------------------------+
|  CHOOSE MISSION …              |  EXCURSION SQUAD (order = spawn, max 4)      |
|  [ mission cards ]             |  +----------------------------------------+  |
|  [ detail ]                    |  | 1. Alice                    [ − ]      |  |
|                                |  | 2. [ empty ]  (drop)  [ v ]             |  |
|                                |  | 3. [ empty ]  (drop)  [ v ]             |  |
|                                |  | 4. [ empty ]  (drop)  [ v ]             |  |
|                                |  +----------------------------------------+  |
|                                |  [ Open preparation ]                        |
+--------------------------------+----------------------------------------------+
|  ROSTER — full width, vertical scroll, all recruited heroes                    |
|  +----------------------------------------------------------------------+    |
|  | ⊕  Alice    excursion                                                  |    |
|  | ⌂  Bob      workbench                                                  |    |
|  | ·  Carol    unassigned                                                 |    |
|  +----------------------------------------------------------------------+    |
|  Legend: ⊕ excursion   ⌂ base job   · unassigned   (glyphs are examples)     |
+------------------------------------------------------------------------------+
|  BASE THIS WEEK …                                                            |
+------------------------------------------------------------------------------+
```

### Related

- State ownership, `PlayerParty`, excursion indices, ability-resource contract: [`scavenger-architecture/reference.md`](../scavenger-architecture/reference.md).

## When structure matters more

Floating vs docked chrome, drag handles, and transfer UI patterns remain in [`SKILL.md`](SKILL.md).
