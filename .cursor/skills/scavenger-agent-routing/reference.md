# Routing reference

Use this only when routing is ambiguous. Keep `SKILL.md` concise.

## Escalation matrix

| If primary is... | Escalate to... | When |
| --- | --- | --- |
| `combat-grid-spatial` | `combat-flow-ai` | Bug is call order, AP/cooldown gate, or selection state |
| `combat-flow-ai` | `combat-grid-spatial` | Fix requires LOS/path/range math changes |
| `ui-panels-inventory` | `combat-rules-economy` | UI needs new computed value or affordability rule |
| `save-persistence` | `content-catalogs` | New saved field stores catalog/registry ids |
| `content-catalogs` | `save-persistence` | Id/schema changes can break old saves |

## Anti-drift checklist

Run this whenever you rename/move files in `Assets/Scripts/`:

1. Update affected manifest file under [`.cursor/agents/`](../../agents/).
2. Update this routing table only if ownership changed.
3. Keep custom subagent prompts short; do not duplicate long file lists there.
4. If uncertain, trust repo structure over stale manifest text and patch the manifest.
