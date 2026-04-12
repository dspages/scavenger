---
name: scavenger-agent-routing
description: >-
  Routes requests to the right Scavenger specialist subagent (or keeps work on
  generalist), with clear handoff rules between combat rules, grid spatial
  logic, turn/controller flow, UI, persistence, and content catalogs.
---

# Scavenger — subagent routing

Use this when deciding which specialist should own a task.

## Quick routing steps

1. Identify the user-visible symptom (wrong number, wrong tile, wrong turn flow, wrong UI, bad save, missing content entry).
2. Choose exactly one primary subagent from the table below.
3. If the change crosses a boundary, keep primary ownership and request one explicit handoff.
4. Skip subagents for trivial one-file edits; use generalist.

## Decision table

| Symptom / request | Primary subagent |
| --- | --- |
| Damage, hit/crit, affordability, spend mismatch | `combat-rules-economy` |
| Reachability, BFS, Manhattan/range, blocked tiles, LOS/vision | `combat-grid-spatial` |
| Turn order, action selection, AP/cooldown wiring, enemy behavior | `combat-flow-ai` |
| Combat/home-base panel behavior, drag/transfer, tooltip rendering | `ui-panels-inventory` |
| Save/load schema, checkpoint persistence, JsonUtility DTO changes | `save-persistence` |
| Item/ability/weapon definitions, registry id wiring | `content-catalogs` |

## Boundary rules

- `combat-rules-economy` owns formulas and cost rules; controllers/UI consume, not reimplement.
- `combat-grid-spatial` owns tile math and spatial queries; controllers/actions call these APIs.
- `combat-flow-ai` owns orchestration and sequencing; delegates spatial math to grid and formulas to rules.

## Source of truth

For each specialist, use its manifest under [`.cursor/agents/`](../../agents/) as the authoritative scope file.
