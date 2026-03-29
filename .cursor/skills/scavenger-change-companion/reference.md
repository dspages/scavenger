# Scavenger — change companion (large / architectural changes)

Companion to [`SKILL.md`](SKILL.md). Use when the change **restructures** the project or **materially** changes how players experience the game.

## When this applies

- New subsystem, moved top-level folders, renamed canonical types, or new primary scenes.
- Changed **save** shape, checkpoint rules, or serialization contracts.
- “This affects how the whole game is wired” — not a localized bugfix.

## Documentation to update

| Area | What to refresh |
| --- | --- |
| **Architecture** | [`scavenger-architecture`](../scavenger-architecture/SKILL.md) **map** and [`reference.md`](../scavenger-architecture/reference.md) — scenes, flows, state ownership, “if you change X” table. |
| **Conventions** | [`scavenger-conventions`](../scavenger-conventions/SKILL.md) — only when the team has **new stable** rules (naming, folders, test layout). |
| **Persistence** | [`scavenger-data-persistence`](../scavenger-data-persistence/SKILL.md) — save model, checkpoint policy, JsonUtility fields, item/ability ids. |
| **Human / players** | [`docs/PLAYER_GUIDE.md`](../../../docs/PLAYER_GUIDE.md) — gameplay loop, controls, stats, abilities when those **meaningfully** change. |
| **Other skills** | Update **debugging**, **UI toolkit**, etc., only when facts they assert are no longer true—surgical edits, not wholesale rewrites. |

## Tests for large changes

- Broader coverage may be justified: multiple cases, or a PlayMode test that loads a **committed** production or test scene when integration risk is high.
- Still prefer the **smallest** suite that proves the new boundaries; avoid redundant layers that duplicate the same assertion.

## Related

- Ordinary regression and light doc rules: [`SKILL.md`](SKILL.md)
- Running tests safely in the Editor: [`scavenger-post-change-verification`](../scavenger-post-change-verification/SKILL.md)
