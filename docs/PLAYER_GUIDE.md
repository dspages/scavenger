# Scavenger — player guide

**Work in progress.** Systems and balance are still evolving; this page is a starting point for how the current build is meant to be played.

## Gameplay loop (overview)

You start from the **main menu**, begin or continue a campaign, and spend time in **home base** preparing your party (stash, squad selection, loadout). Missions send you into **tactical combat** on a grid. After a fight, you return to base and the campaign clock can advance. (Exact mission flow and meta progression are still being built out.)

## How to play — combat basics

Combat is **turn-based** on a square grid. When it is one of your characters’ turns, you spend that character’s **action points (AP)** to **move**, **attack**, use **abilities**, and manage **inventory** until you are done, then press **End Turn** so the next combatant can act.

- **Move:** Click a valid tile to path and move along the highlighted route. Movement costs AP (see below).
- **Act:** Choose an action from the **action bar** at the bottom (weapons, **Kick**, class specials, and learned **abilities**). Valid targets depend on the action—enemies, tiles, yourself, or the ground. Tooltips explain range and cost where implemented.
- **Right-click** during your turn can **clear your selected action** and return toward a default attack-style choice (when applicable).
- **End Turn:** When you have no more AP you want to spend—or you want to hold position—use **End Turn** on the bottom-right of the HUD. Enemies and allies then take their turns in order.

**UI and the map:** The HUD, **Loadout**, and **Combat log** capture mouse input when the pointer is over them. To issue move or attack commands, click **on the battlefield** in an area not covered by those panels (or close/toggle **Inventory** to get the panel out of the way).

## Combat HUD

| Area | What it is |
| --- | --- |
| **Combat log** (lower left) | Scrollable text of recent combat events. You can drag the header to reposition the panel and use the control on the header to minimize it. |
| **Bottom bar** | Holds **AP**, the **action bar** (buttons for available actions), **Inventory** (opens loadout), and **End Turn**. |
| **AP (`AP: …`)** | Shows the **current character’s** remaining action points for this turn. |
| **Action bar** | Buttons for actions you can use right now—e.g. **Kick**, attacks tied to equipped weapons, and **abilities** your character knows. Hover for descriptions where tooltips are wired. |
| **Inventory / Loadout** (panel) | Full-screen-style panel on the right, titled **Loadout**. **Inventory** tab: **Equipment** (paper-doll slots), **Backpack** grid, optional quick stats, and a **trash** slot to destroy items. **Character** tab: core **stats** and a **Skills** list. Use **Inventory** on the HUD bar to open and close it. |

## Action points (AP)

**AP** is your character’s **budget for the current turn** for both **movement** and **actions** (attacks, abilities, etc.).

- At the **start of your turn**, your AP is **refreshed**. The base pool scales with **Speed** (faster characters get a larger pool each turn). **Status effects** can change your AP when a turn begins or during the round—watch the log and UI if rules apply.
- **Moving** along a path spends AP per tile according to the pathfinder (blocked or expensive terrain can cost more).
- **Actions** (attacks, spells, Kick, etc.) have an **AP cost** shown in tooltips where available; when the action finishes, that cost is deducted.
- When you **run out of AP** or choose to stop, use **End Turn**; unused AP do not carry over to your next turn.

## Stats (core attributes)

Characters have **attributes** that feed **health**, **mana**, combat math, and UI-derived stats. Typical meanings in this build:

| Stat | Role (high level) |
| --- | --- |
| **Strength** | Carrying capacity, melee damage, thrown range |
| **Agility** | Dodge, crit, backstab-style damage bonuses |
| **Speed** | Turn order and **size of your AP pool** each turn |
| **Intellect** | How many special abilities you can learn and improve |
| **Endurance** | Health and physical resistances |
| **Perception** | Vision / fog-of-war, ranged accuracy, loot-related bonuses |
| **Willpower** | Mental resistances and **mana pool** |

Exact formulas and caps are subject to change; the **Character** tab in loadout shows derived values (armor, attack, etc.) when available.

## Abilities

**Abilities** are data-driven skills your character **learns** (by id, from catalog content). In combat they appear on the **action bar** when affordable and valid, and are listed under **Skills** on the **Character** tab of **Loadout**. Costs may include **AP** and **mana** depending on the ability.
