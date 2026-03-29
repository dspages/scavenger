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

## When structure matters more

Floating vs docked chrome, drag handles, and transfer UI patterns remain in [`SKILL.md`](SKILL.md).
