# Scavenger specialist subagents

This folder is the source of truth for project-specific specialist prompts.

## Layout

- `manifest-*.md`: scope, boundaries, tests, and handoffs.
- `prompts/*.md`: compact prompt templates that reference manifests + skills.

## Usage pattern

1. Use [`scavenger-agent-routing`](../skills/scavenger-agent-routing/SKILL.md) to select primary specialist.
2. Start with `prompts/<name>.md`.
3. If task crosses boundaries, do one explicit handoff using escalation rules.

## Anti-drift rules

- Keep prompt files short; avoid duplicating long file lists.
- Update the matching `manifest-*.md` when files are renamed/moved/rescoped.
- If manifest and repo structure disagree, trust repo and patch manifest.

## Drift check command

Run this when you rename/move scoped files:

`python .cursor/agents/scripts/validate_manifests.py`
